using CozyWorldGeneration.Layers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace CozyWorldGeneration.Editor
{
    [CustomEditor(typeof(GridManager))]
    [CanEditMultipleObjects]
    public class GridManagerEditor : UnityEditor.Editor
    {
        private VisualElement root;
        private GridManager gridManager;
        private Foldout worldLayersFoldout;
        private Foldout visualLayersFoldout;

        public override VisualElement CreateInspectorGUI()
        {
            gridManager = target as GridManager;
            root = new VisualElement();

            // Add default inspector for grid settings
            CreateGridSettingsSection();

            // Add layer collections
            CreateLayerCollectionSection();

            // Add debug section
            CreateDebugSection();

            return root;
        }

        private void CreateGridSettingsSection()
        {
            var gridSettingsFoldout = new Foldout { text = "Grid Settings", value = true };

            var widthField = new IntegerField("Grid Width")
                { value = serializedObject.FindProperty("gridWidth").intValue };
            widthField.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("gridWidth").intValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            var heightField = new IntegerField("Grid Height")
                { value = serializedObject.FindProperty("gridHeight").intValue };
            heightField.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("gridHeight").intValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            var tileSizeField = new FloatField("Tile Size")
                { value = serializedObject.FindProperty("tileSize").floatValue };
            tileSizeField.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("tileSize").floatValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            gridSettingsFoldout.Add(widthField);
            gridSettingsFoldout.Add(heightField);
            gridSettingsFoldout.Add(tileSizeField);

            root.Add(gridSettingsFoldout);
        }

        private void CreateLayerCollectionSection()
        {
            // World Layers Section
            worldLayersFoldout = CreateCollectionUI("World Layers", gridManager.WorldLayerCollection);
            root.Add(worldLayersFoldout);

            // Visual Layers Section
            visualLayersFoldout = CreateCollectionUI("Visual Layers", gridManager.VisualLayerCollection);
            root.Add(visualLayersFoldout);
        }

        private Foldout CreateCollectionUI(string collectionName, LayerCollection collection)
        {
            var container = new Foldout();
            container.style.marginTop = 10;
            container.style.marginBottom = 10;

            var foldout = new Foldout
            {
                text = collectionName,
                value = collection?.foldoutState ?? true
            };

            foldout.RegisterValueChangedCallback(evt =>
            {
                if (collection != null)
                    collection.foldoutState = evt.newValue;
            });

            if (collection != null)
            {
                // Add Layer button
                var addLayerButton = new Button(() => AddNewLayer(collection, foldout))
                {
                    text = "+ Add Layer"
                };
                addLayerButton.style.marginTop = 5;
                addLayerButton.style.marginBottom = 5;
                foldout.Add(addLayerButton);

                // Display existing layers
                RefreshLayerList(collection, foldout);
            }

            container.Add(foldout);
            return container;
        }

        private void RefreshLayerList(LayerCollection collection, Foldout parentFoldout)
        {
            // Clear existing layer UI (except the add button)
            while (parentFoldout.childCount > 1) parentFoldout.RemoveAt(1);

            if (collection.Layers == null)
                return;

            foreach (var layer in collection.Layers)
            {
                if (layer == null)
                    continue;

                var layerElement = CreateLayerUI(layer, collection, parentFoldout);
                parentFoldout.Add(layerElement);
            }
        }

        private VisualElement CreateLayerUI(WorldLayer layer, LayerCollection collection, Foldout parentFoldout)
        {
            var layerContainer = new VisualElement();
            layerContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            layerContainer.style.marginTop = 5;
            layerContainer.style.marginBottom = 5;
            layerContainer.style.paddingLeft = 5;
            layerContainer.style.paddingRight = 5;
            layerContainer.style.paddingTop = 5;
            layerContainer.style.paddingBottom = 5;
            layerContainer.style.borderBottomLeftRadius = 4;
            layerContainer.style.borderBottomRightRadius = 4;
            layerContainer.style.borderTopLeftRadius = 4;
            layerContainer.style.borderTopRightRadius = 4;

            var headerRow = new VisualElement();
            headerRow.style.flexDirection = FlexDirection.Row;
            headerRow.style.justifyContent = Justify.SpaceBetween;

            var foldout = new Foldout
            {
                text = layer.LayerName,
                value = layer.foldoutState
            };
            foldout.style.flexGrow = 1;

            foldout.RegisterValueChangedCallback(evt => { layer.foldoutState = evt.newValue; });

            var removeButton = new Button(() => RemoveLayer(layer, collection, parentFoldout))
            {
                text = "X"
            };
            removeButton.style.width = 30;

            headerRow.Add(foldout);
            headerRow.Add(removeButton);
            layerContainer.Add(headerRow);

            // Layer properties
            var nameField = new TextField("Layer Name") { value = layer.LayerName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                layer.LayerName = evt.newValue;
                foldout.text = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var enabledToggle = new Toggle("Enabled") { value = layer.IsEnabled };
            enabledToggle.RegisterValueChangedCallback(evt =>
            {
                layer.IsEnabled = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var tileTypeField = new EnumField("Tile Type", layer.TileType);
            tileTypeField.RegisterValueChangedCallback(evt =>
            {
                layer.TileType = (TileType)evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var lockToggle = new Toggle("Lock From Paint") { value = layer.LockFromPaint };
            lockToggle.RegisterValueChangedCallback(evt =>
            {
                layer.LockFromPaint = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var colorField = new ColorField("Layer Color") { value = layer.LayerColor };
            colorField.RegisterValueChangedCallback(evt =>
            {
                layer.LayerColor = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var heightField = new IntegerField("Default Height") { value = layer.DefaultLayerHeight };
            heightField.RegisterValueChangedCallback(evt =>
            {
                layer.DefaultLayerHeight = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            // Preview texture display
            var previewLabel = new Label("Preview Texture:");
            var previewImage = new Image();
            if (layer.PreviewTexture != null)
            {
                previewImage.image = layer.PreviewTexture;
                previewImage.style.width = 128;
                previewImage.style.height = 128;
            }

            var clearButton = new Button(() => ClearLayer(layer))
            {
                text = "Clear Layer"
            };

            foldout.Add(nameField);
            foldout.Add(enabledToggle);
            foldout.Add(tileTypeField);
            foldout.Add(lockToggle);
            foldout.Add(colorField);
            foldout.Add(heightField);
            foldout.Add(previewLabel);
            foldout.Add(previewImage);
            foldout.Add(clearButton);

            return layerContainer;
        }

        private void AddNewLayer(LayerCollection collection, Foldout parentFoldout)
        {
            // Create new WorldLayer asset
            var layer = CreateInstance<WorldLayer>();
            layer.LayerName = $"Layer {collection.Layers.Count + 1}";

            // Save as asset
            var path = EditorUtility.SaveFilePanelInProject(
                "Save World Layer",
                layer.LayerName,
                "asset",
                "Save the WorldLayer asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(layer, path);
                AssetDatabase.SaveAssets();

                collection.AddLayer(layer);
                layer.InitializePreviewTexture(gridManager.Width, gridManager.Height);

                // Mark GridManager as dirty and save
                EditorUtility.SetDirty(gridManager);
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                RefreshLayerList(collection, parentFoldout);
            }
        }

        private void RemoveLayer(WorldLayer layer, LayerCollection collection, Foldout parentFoldout)
        {
            if (EditorUtility.DisplayDialog("Remove Layer",
                    $"Are you sure you want to remove '{layer.LayerName}'?",
                    "Yes", "No"))
            {
                collection.RemoveLayer(layer);

                // Mark GridManager as dirty and save
                EditorUtility.SetDirty(gridManager);
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                RefreshLayerList(collection, parentFoldout);
            }
        }

        private void ClearLayer(WorldLayer layer)
        {
            if (EditorUtility.DisplayDialog("Clear Layer",
                    $"Are you sure you want to clear '{layer.LayerName}'?",
                    "Yes", "No"))
            {
                Debug.Log($"[Editor] Starting clear for layer: {layer.LayerName}");

                layer.ClearPreviewTexture();
                EditorUtility.SetDirty(layer);

                // Directly tell the GridManager to clear tiles
                if (gridManager != null)
                {
                    Debug.Log($"[Editor] GridManager found, clearing tiles from grid");
                    ClearTilesFromLayerInManager(layer);
                }
                else
                {
                    Debug.LogWarning("[Editor] GridManager is null!");
                }

                // Force scene view to repaint
                SceneView.RepaintAll();
            }
        }

        private void ClearTilesFromLayerInManager(WorldLayer layer)
        {
            if (gridManager.WorldGrid == null)
                return;

            var tilesToRemove = new System.Collections.Generic.List<Vector2Int>();

            foreach (var position in gridManager.WorldGrid.GetAllPositions())
            {
                var tile = gridManager.WorldGrid.GetTile(position);
                if (tile != null && tile.SourceLayer == layer) tilesToRemove.Add(position);
            }

            foreach (var position in tilesToRemove) gridManager.WorldGrid.SetTile(position.x, position.y, null);

            Debug.Log($"[Editor] Cleared {tilesToRemove.Count} tiles from grid");
        }

        private void CreateDebugSection()
        {
            var debugFoldout = new Foldout { text = "Debug", value = false };

            var drawGizmosToggle = new Toggle("Draw Gizmos")
            {
                value = serializedObject.FindProperty("drawGizmos").boolValue
            };
            drawGizmosToggle.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("drawGizmos").boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            var drawWorldToggle = new Toggle("Draw World Grid")
            {
                value = serializedObject.FindProperty("drawWorldGrid").boolValue
            };
            drawWorldToggle.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("drawWorldGrid").boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            var drawVisualToggle = new Toggle("Draw Visual Grid")
            {
                value = serializedObject.FindProperty("drawVisualGrid").boolValue
            };
            drawVisualToggle.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("drawVisualGrid").boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            debugFoldout.Add(drawGizmosToggle);
            debugFoldout.Add(drawWorldToggle);
            debugFoldout.Add(drawVisualToggle);

            var worldGridColorPicker = new ColorField("World Grid Color")
            {
                value = serializedObject.FindProperty("worldGridColor").colorValue
            };
            worldGridColorPicker.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("worldGridColor").colorValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            var visualGridColorPicker = new ColorField("Visual Grid Color")
            {
                value = serializedObject.FindProperty("visualGridColor").colorValue
            };
            visualGridColorPicker.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("visualGridColor").colorValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            debugFoldout.Add(worldGridColorPicker);
            debugFoldout.Add(visualGridColorPicker);

            root.Add(debugFoldout);
        }
    }
}