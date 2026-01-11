using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using CozyWorldGeneration.Core;
using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Data.Fluids;
using CozyWorldGeneration.Data.Layers;

namespace CozyWorldGeneration.Editor
{
    [Overlay(typeof(SceneView), "Grid Painter")]
    public class GridPainterOverlay : Overlay
    {
        private GridManager activeGridManager;
        private WorldLayer selectedLayer;
        private FluidType selectedFluidType;
        private PaintMode paintMode = PaintMode.Terrain;

        private VisualElement root;
        private ScrollView layerScrollView;
        private VisualElement fluidSettingsContainer;
        private VisualElement terrainSettingsContainer;

        private int brushSize = 1;
        private int fluidAmount = 7;
        private bool placeAsSource = false;

        public int GetBrushSize()
        {
            return brushSize;
        }

        public PaintMode GetPaintMode()
        {
            return paintMode;
        }

        public FluidType GetSelectedFluidType()
        {
            return selectedFluidType;
        }

        public int GetFluidAmount()
        {
            return fluidAmount;
        }

        public bool GetPlaceAsSource()
        {
            return placeAsSource;
        }

        public WorldLayer GetSelectedLayer()
        {
            return selectedLayer;
        }

        public GridManager GetActiveGridManager()
        {
            return activeGridManager;
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
            selectedFluidType = null;
            activeGridManager = null;
        }

        public override VisualElement CreatePanelContent()
        {
            root = new VisualElement();
            root.style.minWidth = 220;
            root.style.maxWidth = 280;
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

            // Paint Mode Toggle
            var modeContainer = new VisualElement();
            modeContainer.style.flexDirection = FlexDirection.Row;
            modeContainer.style.marginBottom = 10;

            var terrainModeBtn = new Button(() => SetPaintMode(PaintMode.Terrain)) { text = "Terrain" };
            terrainModeBtn.style.flexGrow = 1;

            var fluidModeBtn = new Button(() => SetPaintMode(PaintMode.Fluid)) { text = "Fluid" };
            fluidModeBtn.style.flexGrow = 1;

            modeContainer.Add(terrainModeBtn);
            modeContainer.Add(fluidModeBtn);
            root.Add(modeContainer);

            // Info section
            var infoLabel = new Label("Select a GridManager in the scene");
            infoLabel.style.fontSize = 10;
            infoLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            infoLabel.style.whiteSpace = WhiteSpace.Normal;
            infoLabel.style.marginBottom = 10;
            root.Add(infoLabel);

            // Refresh button
            var refreshButton = new Button(RefreshGridManager) { text = "Refresh" };
            refreshButton.style.marginBottom = 10;
            root.Add(refreshButton);

            // Brush Size
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

            brushSizeContainer.Add(brushSizeLabel);
            brushSizeContainer.Add(brushSizeSlider);
            root.Add(brushSizeContainer);

            // Terrain Settings Container
            terrainSettingsContainer = new VisualElement();
            root.Add(terrainSettingsContainer);

            // Fluid Settings Container
            fluidSettingsContainer = new VisualElement();
            fluidSettingsContainer.style.display = DisplayStyle.None;
            CreateFluidSettings();
            root.Add(fluidSettingsContainer);

            // Layer scroll view
            layerScrollView = new ScrollView();
            layerScrollView.style.maxHeight = 300;
            root.Add(layerScrollView);

            RefreshGridManager();
            UpdateModeDisplay();

            return root;
        }

        private void CreateFluidSettings()
        {
            var fluidLabel = new Label("Fluid Settings");
            fluidLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            fluidLabel.style.color = Color.white;
            fluidLabel.style.marginBottom = 5;
            fluidSettingsContainer.Add(fluidLabel);

            // Fluid Amount
            var amountLabel = new Label($"Amount: {fluidAmount}/7");
            amountLabel.style.color = Color.white;

            var amountSlider = new SliderInt(1, 7) { value = fluidAmount };
            amountSlider.RegisterValueChangedCallback(evt =>
            {
                fluidAmount = evt.newValue;
                amountLabel.text = $"Amount: {fluidAmount}/7";
            });

            fluidSettingsContainer.Add(amountLabel);
            fluidSettingsContainer.Add(amountSlider);

            // Source Toggle
            var sourceToggle = new Toggle("Place as Source") { value = placeAsSource };
            sourceToggle.style.marginTop = 5;
            sourceToggle.RegisterValueChangedCallback(evt => { placeAsSource = evt.newValue; });
            fluidSettingsContainer.Add(sourceToggle);

            // Clear Fluids Button
            var clearFluidsBtn = new Button(() => ClearAllFluids()) { text = "Clear All Fluids" };
            clearFluidsBtn.style.marginTop = 10;
            clearFluidsBtn.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);
            fluidSettingsContainer.Add(clearFluidsBtn);
        }

        private void SetPaintMode(PaintMode mode)
        {
            paintMode = mode;
            UpdateModeDisplay();
            RefreshUI();
        }

        private void UpdateModeDisplay()
        {
            if (terrainSettingsContainer != null && fluidSettingsContainer != null)
            {
                terrainSettingsContainer.style.display =
                    paintMode == PaintMode.Terrain ? DisplayStyle.Flex : DisplayStyle.None;
                fluidSettingsContainer.style.display =
                    paintMode == PaintMode.Fluid ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void RefreshGridManager()
        {
            if (Selection.activeGameObject != null)
                activeGridManager = Selection.activeGameObject.GetComponent<GridManager>();

            if (activeGridManager == null)
                activeGridManager = Object.FindAnyObjectByType<GridManager>();

            RefreshUI();
        }

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

            var gridInfoLabel = new Label($"Grid: {activeGridManager.Width}x{activeGridManager.Height}");
            gridInfoLabel.style.fontSize = 11;
            gridInfoLabel.style.marginBottom = 10;
            gridInfoLabel.style.color = new Color(0.8f, 0.8f, 0.8f);
            layerScrollView.Add(gridInfoLabel);

            if (paintMode == PaintMode.Terrain)
                RefreshTerrainLayers();
            else
                RefreshFluidTypes();
        }

        private void RefreshTerrainLayers()
        {
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

        private void RefreshFluidTypes()
        {
            var fluidTypesLabel = new Label("Fluid Types");
            fluidTypesLabel.style.fontSize = 12;
            fluidTypesLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            fluidTypesLabel.style.marginTop = 5;
            fluidTypesLabel.style.marginBottom = 5;
            fluidTypesLabel.style.color = Color.white;
            layerScrollView.Add(fluidTypesLabel);

            // Find all FluidType assets
            var guids = AssetDatabase.FindAssets("t:FluidType");

            if (guids.Length == 0)
            {
                var noFluidsLabel =
                    new Label(
                        "No FluidType assets found.\nCreate one via:\nCreate > Cozy World Generation > Fluid Type");
                noFluidsLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
                noFluidsLabel.style.whiteSpace = WhiteSpace.Normal;
                layerScrollView.Add(noFluidsLabel);
                return;
            }

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var fluidType = AssetDatabase.LoadAssetAtPath<FluidType>(path);

                if (fluidType != null)
                {
                    var fluidElement = CreateFluidTypeButton(fluidType);
                    layerScrollView.Add(fluidElement);
                }
            }

            if (selectedFluidType != null)
            {
                var selectedInfoLabel = new Label($"Active: {selectedFluidType.FluidName}");
                selectedInfoLabel.style.fontSize = 11;
                selectedInfoLabel.style.marginTop = 10;
                selectedInfoLabel.style.color = new Color(0.5f, 0.8f, 1f);
                selectedInfoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                layerScrollView.Add(selectedInfoLabel);
            }
        }

        private VisualElement CreateLayerButton(WorldLayer layer)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginBottom = 3;
            container.style.justifyContent = Justify.SpaceBetween;

            var layerButton = new Button(() => SelectLayer(layer));
            layerButton.style.flexGrow = 1;
            layerButton.style.marginRight = 3;

            if (layer == selectedLayer)
                layerButton.style.backgroundColor = new Color(0.3f, 0.5f, 0.3f);
            else if (!layer.IsEnabled)
                layerButton.style.backgroundColor = new Color(0.3f, 0.2f, 0.2f);
            else if (layer.LockFromPaint)
                layerButton.style.backgroundColor = new Color(0.4f, 0.3f, 0.2f);

            var buttonContent = new VisualElement();
            buttonContent.style.flexDirection = FlexDirection.Row;
            buttonContent.style.alignItems = Align.Center;

            var colorBox = new VisualElement();
            colorBox.style.width = 16;
            colorBox.style.height = 16;
            colorBox.style.backgroundColor = layer.LayerColor;
            colorBox.style.marginRight = 5;
            colorBox.style.borderBottomLeftRadius = 2;
            colorBox.style.borderBottomRightRadius = 2;
            colorBox.style.borderTopLeftRadius = 2;
            colorBox.style.borderTopRightRadius = 2;

            var nameLabel = new Label(layer.LayerName);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 10;

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

            var clearButton = new Button(() => ClearLayerData(layer)) { text = "Clear" };
            clearButton.style.width = 50;
            clearButton.style.fontSize = 9;

            container.Add(layerButton);
            container.Add(clearButton);

            return container;
        }

        private VisualElement CreateFluidTypeButton(FluidType fluidType)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.marginBottom = 3;

            var fluidButton = new Button(() => SelectFluidType(fluidType));
            fluidButton.style.flexGrow = 1;

            if (fluidType == selectedFluidType)
                fluidButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.5f);

            var buttonContent = new VisualElement();
            buttonContent.style.flexDirection = FlexDirection.Row;
            buttonContent.style.alignItems = Align.Center;

            var colorBox = new VisualElement();
            colorBox.style.width = 16;
            colorBox.style.height = 16;
            colorBox.style.backgroundColor = fluidType.Color;
            colorBox.style.marginRight = 5;
            colorBox.style.borderBottomLeftRadius = 2;
            colorBox.style.borderBottomRightRadius = 2;
            colorBox.style.borderTopLeftRadius = 2;
            colorBox.style.borderTopRightRadius = 2;

            var nameLabel = new Label(fluidType.FluidName);
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 10;

            buttonContent.Add(colorBox);
            buttonContent.Add(nameLabel);
            fluidButton.Add(buttonContent);

            container.Add(fluidButton);

            return container;
        }

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

        private void SelectFluidType(FluidType fluidType)
        {
            selectedFluidType = fluidType;
            RefreshUI();
            Debug.Log($"Selected fluid type: {fluidType.FluidName}");
        }

        private void ClearLayerData(WorldLayer layer)
        {
            if (EditorUtility.DisplayDialog("Clear Layer",
                    $"Clear all painted data from '{layer.LayerName}'?", "Yes", "No"))
            {
                layer.ClearPreviewTexture();
                EditorUtility.SetDirty(layer);

                if (activeGridManager == null)
                    RefreshGridManager();

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

        private void ClearAllFluids()
        {
            if (activeGridManager?.WorldGrid == null)
            {
                Debug.LogWarning("No WorldGrid found");
                return;
            }

            if (EditorUtility.DisplayDialog("Clear All Fluids",
                    "Clear all fluid data?", "Yes", "No"))
            {
                var fluidPositions = new List<Vector3Int>();

                foreach (var position in activeGridManager.WorldGrid.GetAllPositions())
                {
                    var tile = activeGridManager.WorldGrid.GetTile(position);
                    if (tile?.HasFluid == true)
                        fluidPositions.Add(position);
                }

                foreach (var pos in fluidPositions)
                    activeGridManager.WorldGrid.RemoveFluid(pos.x, pos.y, pos.z);

                SceneView.RepaintAll();
                Debug.Log($"[Overlay] Cleared {fluidPositions.Count} fluid tiles");
            }
        }
    }
}