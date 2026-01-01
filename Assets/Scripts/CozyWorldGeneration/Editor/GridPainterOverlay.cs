using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using CozyWorldGeneration.Core;
using CozyWorldGeneration.Data.Layers;

namespace CozyWorldGeneration.Editor
{
    /// <summary>
    /// Overlay widget that appears in the Scene View for painting on grid layers.
    /// </summary>
    [Overlay(typeof(SceneView), "Grid Painter")]
    public class GridPainterOverlay : Overlay
    {
        private GridManager activeGridManager;
        private WorldLayer selectedLayer;
        private VisualElement root;
        private ScrollView layerScrollView;
        private int brushSize = 1;

        public int GetBrushSize()
        {
            return brushSize;
        }

        public override void OnCreated()
        {
            base.OnCreated();
            RefreshGridManager();
        }

        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
            selectedLayer = null;
            activeGridManager = null;
        }

        public override VisualElement CreatePanelContent()
        {
            root = new VisualElement();
            root.style.minWidth = 200;
            root.style.maxWidth = 250;
            root.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.95f);
            root.style.paddingLeft = 5;
            root.style.paddingRight = 5;
            root.style.paddingTop = 5;
            root.style.paddingBottom = 5;
            root.style.borderBottomLeftRadius = 5;
            root.style.borderBottomRightRadius = 5;
            root.style.borderTopLeftRadius = 5;
            root.style.borderTopRightRadius = 5;

            // Title
            var titleLabel = new Label("Grid Painter");
            titleLabel.style.fontSize = 14;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 10;
            titleLabel.style.color = Color.white;
            root.Add(titleLabel);

            // Info section
            var infoLabel = new Label("Select a GridManager in the scene");
            infoLabel.style.fontSize = 10;
            infoLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            infoLabel.style.marginBottom = 10;
            root.Add(infoLabel);

            // Refresh button
            var refreshButton = new Button(RefreshGridManager)
            {
                text = "Refresh"
            };
            refreshButton.style.marginBottom = 10;
            root.Add(refreshButton);

            // Brush Size slider — ADD THIS SECTION
            var brushSizeContainer = new VisualElement();
            brushSizeContainer.style.marginBottom = 10;

            var brushSizeLabel = new Label($"Brush Size: {brushSize}");
            brushSizeLabel.style.color = Color.white;
            brushSizeLabel.style.marginBottom = 3;

            var brushSizeSlider = new SliderInt(1, 10) { value = brushSize };
            brushSizeSlider.RegisterValueChangedCallback(evt =>
            {
                brushSize = evt.newValue;
                brushSizeLabel.text = $"Brush Size: {brushSize}";
                SceneView.RepaintAll();
            });

            var brushSizeHint = new Label("(Shift + Scroll to adjust)");
            brushSizeHint.style.fontSize = 9;
            brushSizeHint.style.color = new Color(0.5f, 0.5f, 0.5f);

            brushSizeContainer.Add(brushSizeLabel);
            brushSizeContainer.Add(brushSizeSlider);
            brushSizeContainer.Add(brushSizeHint);
            root.Add(brushSizeContainer);

            // Scroll view for layers
            layerScrollView = new ScrollView();
            layerScrollView.style.maxHeight = 400;
            root.Add(layerScrollView);

            RefreshGridManager();

            return root;
        }


        /// <summary>
        /// Finds the active GridManager in the scene and updates the UI.
        /// </summary>
        private void RefreshGridManager()
        {
            // Find GridManager in selection or scene
            if (Selection.activeGameObject != null)
                activeGridManager = Selection.activeGameObject.GetComponent<GridManager>();

            if (activeGridManager == null) activeGridManager = Object.FindAnyObjectByType<GridManager>();

            RefreshUI();
        }

        /// <summary>
        /// Refreshes the entire UI based on current GridManager state.
        /// </summary>
        private void RefreshUI()
        {
            if (layerScrollView == null)
                return;

            layerScrollView.Clear();

            if (activeGridManager == null)
            {
                var noGridLabel = new Label("No GridManager found in scene");
                noGridLabel.style.color = new Color(1f, 0.5f, 0.5f);
                layerScrollView.Add(noGridLabel);
                return;
            }

            // Display grid info
            var gridInfoLabel = new Label($"Grid: {activeGridManager.Width}x{activeGridManager.Height}");
            gridInfoLabel.style.fontSize = 11;
            gridInfoLabel.style.marginBottom = 10;
            gridInfoLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            layerScrollView.Add(gridInfoLabel);

            // World Layers Section
            if (activeGridManager.WorldLayerCollection != null &&
                activeGridManager.WorldLayerCollection.Layers.Count > 0)
            {
                var worldLayersLabel = new Label("World Layers");
                worldLayersLabel.style.fontSize = 12;
                worldLayersLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                worldLayersLabel.style.marginTop = 5;
                worldLayersLabel.style.marginBottom = 5;
                worldLayersLabel.style.color = Color.white;
                layerScrollView.Add(worldLayersLabel);

                foreach (var layer in activeGridManager.WorldLayerCollection.Layers)
                    if (layer != null)
                    {
                        var layerElement = CreateLayerButton(layer);
                        layerScrollView.Add(layerElement);
                    }
            }

            // Selected layer info
            if (selectedLayer != null)
            {
                var selectedInfoLabel = new Label($"Active: {selectedLayer.LayerName}");
                selectedInfoLabel.style.fontSize = 11;
                selectedInfoLabel.style.marginTop = 10;
                selectedInfoLabel.style.color = new Color(0.5f, 1f, 0.5f);
                selectedInfoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                layerScrollView.Add(selectedInfoLabel);
            }
        }

        /// <summary>
        /// Creates a button element for a layer with clear functionality.
        /// </summary>
        private VisualElement CreateLayerButton(WorldLayer layer)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginBottom = 3;
            container.style.justifyContent = Justify.SpaceBetween;

            // Layer selection button
            var layerButton = new Button(() => SelectLayer(layer));
            layerButton.style.flexGrow = 1;
            layerButton.style.marginRight = 3;

            // Visual styling based on layer state
            if (layer == selectedLayer)
                layerButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.3f);
            else if (!layer.IsEnabled)
                layerButton.style.backgroundColor = new Color(0.3f, 0.2f, 0.2f);
            else if (layer.LockFromPaint) layerButton.style.backgroundColor = new Color(0.4f, 0.3f, 0.2f);

            // Button content
            var buttonContent = new VisualElement();
            buttonContent.style.flexDirection = FlexDirection.Row;
            buttonContent.style.alignItems = Align.Center;

            // Color indicator
            var colorBox = new VisualElement();
            colorBox.style.width = 16;
            colorBox.style.height = 16;
            colorBox.style.backgroundColor = layer.LayerColor;
            colorBox.style.marginRight = 5;
            colorBox.style.borderBottomLeftRadius = 2;
            colorBox.style.borderBottomRightRadius = 2;
            colorBox.style.borderTopLeftRadius = 2;
            colorBox.style.borderTopRightRadius = 2;

            // Layer name
            var nameLabel = new Label(layer.LayerName);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 10;

            // Status icons
            var statusText = "";
            if (!layer.IsEnabled) statusText += " [OFF]";
            if (layer.LockFromPaint) statusText += " 🔒";

            if (!string.IsNullOrEmpty(statusText))
            {
                var statusLabel = new Label(statusText);
                statusLabel.style.fontSize = 9;
                statusLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
                buttonContent.Add(statusLabel);
            }

            buttonContent.Add(colorBox);
            buttonContent.Add(nameLabel);
            layerButton.Add(buttonContent);

            // Clear button
            var clearButton = new Button(() => ClearLayerData(layer))
            {
                text = "Clear"
            };
            clearButton.style.width = 50;
            clearButton.style.fontSize = 9;

            container.Add(layerButton);
            container.Add(clearButton);

            return container;
        }

        /// <summary>
        /// Selects a layer for painting.
        /// </summary>
        private void SelectLayer(WorldLayer layer)
        {
            if (!layer.IsEnabled)
            {
                EditorUtility.DisplayDialog("Layer Disabled",
                    $"Layer '{layer.LayerName}' is disabled. Enable it first.", "OK");
                return;
            }

            if (layer.LockFromPaint)
            {
                EditorUtility.DisplayDialog("Layer Locked",
                    $"Layer '{layer.LayerName}' is locked from painting.", "OK");
                return;
            }

            selectedLayer = layer;
            RefreshUI();

            Debug.Log($"Selected layer for painting: {layer.LayerName}");
        }

        /// <summary>
        /// Clears all painted data from a layer.
        /// </summary>
        private void ClearLayerData(WorldLayer layer)
        {
            if (EditorUtility.DisplayDialog("Clear Layer",
                    $"Clear all painted data from '{layer.LayerName}'?", "Yes", "No"))
            {
                layer.ClearPreviewTexture();
                EditorUtility.SetDirty(layer);

                if (activeGridManager == null) RefreshGridManager();

                if (activeGridManager != null && activeGridManager.WorldGrid != null)
                {
                    var tilesToRemove = new List<Vector3Int>();

                    foreach (var position in activeGridManager.WorldGrid.GetAllPositions())
                    {
                        var tile = activeGridManager.WorldGrid.GetTile(position.x, position.y, position.z);
                        if (tile != null && tile.SourceLayer == layer)
                            tilesToRemove.Add(position);
                    }

                    foreach (var pos in tilesToRemove)
                        activeGridManager.WorldGrid.RemoveTile(pos.x, pos.y, pos.z);

                    Debug.Log($"[Overlay] Cleared layer: {layer.LayerName} - Removed {tilesToRemove.Count} tiles");
                }

                SceneView.RepaintAll();
            }
        }

        /// <summary>
        /// Gets the currently selected layer for painting.
        /// </summary>
        public WorldLayer GetSelectedLayer()
        {
            return selectedLayer;
        }

        /// <summary>
        /// Gets the active GridManager.
        /// </summary>
        public GridManager GetActiveGridManager()
        {
            return activeGridManager;
        }
    }
}