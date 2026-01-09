using System.Collections.Generic;
using CozyWorldGeneration.Core;
using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Data.Tilesets;
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

        private void OnEnable()
        {
            gridManager = (GridManager)target;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            if (gridManager == null || gridManager.WorldGrid == null)
                return;

            if (gridManager.WorldLayerCollection?.Layers != null)
                foreach (var layer in gridManager.WorldLayerCollection.Layers)
                    layer?.ForceRebuildTexture(gridManager.Width, gridManager.Height);

            RebuildWorldGridFromLayers();
            SceneView.RepaintAll();
        }

        private void RebuildWorldGridFromLayers()
        {
            if (gridManager.WorldLayerCollection == null) return;

            gridManager.WorldGrid.SuppressEvents = true;
            gridManager.WorldGrid.Clear();

            foreach (var layer in gridManager.WorldLayerCollection.Layers)
            {
                if (layer == null) continue;

                if (layer.PreviewTexture == null)
                {
                    Debug.LogWarning($"[GridManager] Layer '{layer.LayerName}' has no PreviewTexture, skipping.");
                    continue;
                }

                for (var x = 0; x < layer.PreviewTexture.width; x++)
                for (var y = 0; y < layer.PreviewTexture.height; y++)
                    if (layer.IsPixelPainted(x, y))
                        gridManager.WorldGrid.PlaceTile(x, y, layer);
            }

            gridManager.WorldGrid.SuppressEvents = false;
            gridManager.RefreshAllVisualGrids();
        }

        public override VisualElement CreateInspectorGUI()
        {
            gridManager = target as GridManager;
            root = new VisualElement();

            CreateGridSettingsSection();
            CreateFluidSettingsSection();
            CreateLayerCollectionSection();
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

        private void CreateFluidSettingsSection()
        {
            var fluidSettingsFoldout = new Foldout { text = "Fluid Settings", value = true };
            fluidSettingsFoldout.style.marginTop = 10;

            // Enable Fluids Toggle
            var enableFluidsToggle = new Toggle("Enable Fluids")
            {
                value = serializedObject.FindProperty("enableFluids").boolValue
            };
            enableFluidsToggle.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("enableFluids").boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

            // Tick Rate
            var tickRateField = new FloatField("Tick Rate")
            {
                value = serializedObject.FindProperty("fluidTickRate").floatValue
            };
            tickRateField.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("fluidTickRate").floatValue = Mathf.Max(0.1f, evt.newValue);
                serializedObject.ApplyModifiedProperties();
            });

            var tickRateHint = new Label("Simulations per second");
            tickRateHint.style.fontSize = 10;
            tickRateHint.style.color = new Color(0.6f, 0.6f, 0.6f);
            tickRateHint.style.marginBottom = 5;

            // Max Levels
            var maxLevelsField = new IntegerField("Max Fluid Levels")
            {
                value = serializedObject.FindProperty("maxFluidLevels").intValue
            };
            maxLevelsField.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("maxFluidLevels").intValue = Mathf.Max(1, evt.newValue);
                serializedObject.ApplyModifiedProperties();
            });

            // Runtime Info
            var runtimeInfoContainer = new VisualElement();
            runtimeInfoContainer.style.marginTop = 10;
            runtimeInfoContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            runtimeInfoContainer.style.paddingTop = 5;
            runtimeInfoContainer.style.paddingBottom = 5;
            runtimeInfoContainer.style.paddingLeft = 5;
            runtimeInfoContainer.style.paddingRight = 5;
            runtimeInfoContainer.style.borderBottomLeftRadius = 4;
            runtimeInfoContainer.style.borderBottomRightRadius = 4;
            runtimeInfoContainer.style.borderTopLeftRadius = 4;
            runtimeInfoContainer.style.borderTopRightRadius = 4;

            var runtimeLabel = new Label("Runtime Info");
            runtimeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            runtimeLabel.style.marginBottom = 5;

            var fluidTileCountLabel = new Label("Fluid Tiles: --");
            var fluidBodyCountLabel = new Label("Fluid Bodies: --");

            // Update runtime info periodically
            runtimeInfoContainer.schedule.Execute(() =>
            {
                if (gridManager != null && gridManager.FluidSimulator != null)
                {
                    var fluidGrid = gridManager.FluidSimulator.fluidGrid;
                    if (fluidGrid != null) fluidTileCountLabel.text = $"Fluid Tiles: {fluidGrid.GetTileCount()}";
                    fluidBodyCountLabel.text = $"Fluid Bodies: {gridManager.FluidSimulator.BodyCount}";
                }
                else
                {
                    fluidTileCountLabel.text = "Fluid Tiles: (not running)";
                    fluidBodyCountLabel.text = "Fluid Bodies: (not running)";
                }
            }).Every(500);

            runtimeInfoContainer.Add(runtimeLabel);
            runtimeInfoContainer.Add(fluidTileCountLabel);
            runtimeInfoContainer.Add(fluidBodyCountLabel);

            // Clear Fluids Button
            var clearFluidsBtn = new Button(() =>
            {
                if (gridManager.FluidSimulator?.fluidGrid == null)
                {
                    EditorUtility.DisplayDialog("No Fluids", "FluidSimulator not initialized", "OK");
                    return;
                }

                if (EditorUtility.DisplayDialog("Clear All Fluids",
                        "Clear all fluid data?", "Yes", "No"))
                {
                    gridManager.FluidSimulator.fluidGrid.Clear();
                    SceneView.RepaintAll();
                    Debug.Log("[GridManager] Cleared all fluids");
                }
            })
            {
                text = "Clear All Fluids"
            };
            clearFluidsBtn.style.marginTop = 10;
            clearFluidsBtn.style.backgroundColor = new Color(0.5f, 0.2f, 0.2f);

            fluidSettingsFoldout.Add(enableFluidsToggle);
            fluidSettingsFoldout.Add(tickRateField);
            fluidSettingsFoldout.Add(tickRateHint);
            fluidSettingsFoldout.Add(maxLevelsField);
            fluidSettingsFoldout.Add(runtimeInfoContainer);
            fluidSettingsFoldout.Add(clearFluidsBtn);

            root.Add(fluidSettingsFoldout);
        }

        private void CreateLayerCollectionSection()
        {
            // World Layers
            worldLayersFoldout = new Foldout { text = "World Layers", value = true };
            worldLayersFoldout.style.marginTop = 10;

            var addExistingWorldLayer = new ObjectField("Add Existing Layer")
            {
                objectType = typeof(WorldLayer),
                value = null
            };
            addExistingWorldLayer.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != null)
                {
                    var layer = evt.newValue as WorldLayer;
                    if (!gridManager.WorldLayerCollection.Layers.Contains(layer))
                    {
                        gridManager.WorldLayerCollection.AddLayer(layer);
                        layer.InitializePreviewTexture(gridManager.Width, gridManager.Height);
                        EditorUtility.SetDirty(gridManager);
                        RefreshWorldLayers();
                    }

                    addExistingWorldLayer.SetValueWithoutNotify(null);
                }
            });
            worldLayersFoldout.Add(addExistingWorldLayer);

            var addWorldLayerBtn = new Button(() => AddNewWorldLayer())
            {
                text = "+ Create New World Layer"
            };
            addWorldLayerBtn.style.marginBottom = 5;
            worldLayersFoldout.Add(addWorldLayerBtn);

            RefreshWorldLayers();
            root.Add(worldLayersFoldout);

            // Visual Layers
            visualLayersFoldout = new Foldout { text = "Visual Layers", value = true };
            visualLayersFoldout.style.marginTop = 10;

            var addExistingVisualLayer = new ObjectField("Add Existing Layer")
            {
                objectType = typeof(VisualLayer),
                value = null
            };
            addExistingVisualLayer.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue != null)
                {
                    var layer = evt.newValue as VisualLayer;
                    if (!gridManager.VisualLayerCollection.Layers.Contains(layer))
                    {
                        gridManager.VisualLayerCollection.AddLayer(layer);
                        EditorUtility.SetDirty(gridManager);
                        RefreshVisualLayers();
                    }

                    addExistingVisualLayer.SetValueWithoutNotify(null);
                }
            });
            visualLayersFoldout.Add(addExistingVisualLayer);

            var addVisualLayerBtn = new Button(() => AddNewVisualLayer())
            {
                text = "+ Create New Visual Layer"
            };
            addVisualLayerBtn.style.marginBottom = 5;
            visualLayersFoldout.Add(addVisualLayerBtn);

            RefreshVisualLayers();
            root.Add(visualLayersFoldout);
        }

        private void RefreshWorldLayers()
        {
            while (worldLayersFoldout.childCount > 2)
                worldLayersFoldout.RemoveAt(2);

            if (gridManager.WorldLayerCollection?.Layers == null)
                return;

            foreach (var layer in gridManager.WorldLayerCollection.Layers)
            {
                var worldLayer = layer as WorldLayer;
                if (worldLayer != null)
                    worldLayersFoldout.Add(CreateWorldLayerUI(worldLayer));
            }
        }

        private void RefreshVisualLayers()
        {
            while (visualLayersFoldout.childCount > 2)
                visualLayersFoldout.RemoveAt(2);

            if (gridManager.VisualLayerCollection?.Layers == null)
                return;

            foreach (var layer in gridManager.VisualLayerCollection.Layers)
            {
                var visualLayer = layer as VisualLayer;
                if (visualLayer != null)
                    visualLayersFoldout.Add(CreateVisualLayerUI(visualLayer));
            }
        }

        private VisualElement CreateWorldLayerUI(WorldLayer layer)
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            container.style.marginTop = 5;
            container.style.paddingTop = 5;
            container.style.paddingBottom = 5;
            container.style.paddingLeft = 5;
            container.style.paddingRight = 5;
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.FlexStart;

            var foldout = new Foldout { text = layer.LayerName, value = layer.foldoutState };
            foldout.style.flexGrow = 1;
            foldout.RegisterValueChangedCallback(evt => layer.foldoutState = evt.newValue);

            var removeBtn = new Button(() => RemoveWorldLayer(layer)) { text = "X" };
            removeBtn.style.width = 24;
            removeBtn.style.height = 18;
            removeBtn.style.marginLeft = 5;
            removeBtn.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);

            header.Add(foldout);
            header.Add(removeBtn);
            container.Add(header);

            var contentContainer = new VisualElement();
            contentContainer.style.flexDirection = FlexDirection.Row;

            var propertiesContainer = new VisualElement();
            propertiesContainer.style.flexGrow = 1;
            propertiesContainer.style.marginRight = 10;

            var nameField = new TextField("Name") { value = layer.LayerName };
            nameField.RegisterValueChangedCallback(evt =>
            {
                layer.LayerName = evt.newValue;
                foldout.text = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var levelField = new IntegerField("Layer Level") { value = layer.LayerLevel };
            levelField.RegisterValueChangedCallback(evt =>
            {
                layer.LayerLevel = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var enabledToggle = new Toggle("Enabled") { value = layer.IsEnabled };
            enabledToggle.RegisterValueChangedCallback(evt =>
            {
                layer.IsEnabled = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var lockToggle = new Toggle("Lock From Paint") { value = layer.LockFromPaint };
            lockToggle.RegisterValueChangedCallback(evt =>
            {
                layer.LockFromPaint = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var colorField = new ColorField("Color") { value = layer.LayerColor };
            colorField.RegisterValueChangedCallback(evt =>
            {
                layer.LayerColor = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var clearBtn = new Button(() => ClearLayer(layer)) { text = "Clear Layer" };
            clearBtn.style.marginTop = 5;

            propertiesContainer.Add(nameField);
            propertiesContainer.Add(levelField);
            propertiesContainer.Add(enabledToggle);
            propertiesContainer.Add(lockToggle);
            propertiesContainer.Add(colorField);
            propertiesContainer.Add(clearBtn);

            var textureContainer = new VisualElement();
            textureContainer.style.alignItems = Align.Center;

            var textureImage = new Image();
            textureImage.style.width = 110;
            textureImage.style.height = 110;
            textureImage.style.borderTopWidth = 1;
            textureImage.style.borderBottomWidth = 1;
            textureImage.style.borderLeftWidth = 1;
            textureImage.style.borderRightWidth = 1;
            textureImage.style.borderTopColor = Color.gray;
            textureImage.style.borderBottomColor = Color.gray;
            textureImage.style.borderLeftColor = Color.gray;
            textureImage.style.borderRightColor = Color.gray;
            textureImage.scaleMode = ScaleMode.ScaleToFit;

            if (layer.PreviewTexture != null)
                textureImage.image = layer.PreviewTexture;

            var refreshTextureBtn = new Button(() =>
            {
                layer.ForceRebuildTexture(gridManager.Width, gridManager.Height);
                textureImage.image = layer.PreviewTexture;
                gridManager.RefreshVisualGridForLayer(layer);
                SceneView.RepaintAll();
            })
            {
                text = "Refresh"
            };
            refreshTextureBtn.style.marginTop = 3;

            textureContainer.Add(textureImage);
            textureContainer.Add(refreshTextureBtn);
            contentContainer.Add(textureContainer);
            contentContainer.Add(propertiesContainer);
            foldout.Add(contentContainer);

            return container;
        }

        private VisualElement CreateVisualLayerUI(VisualLayer layer)
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.3f);
            container.style.marginTop = 5;
            container.style.paddingTop = 5;
            container.style.paddingBottom = 5;
            container.style.paddingLeft = 5;
            container.style.paddingRight = 5;
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.FlexStart;

            var foldout = new Foldout { text = layer.LayerName, value = layer.foldoutState };
            foldout.style.flexGrow = 1;
            foldout.RegisterValueChangedCallback(evt => layer.foldoutState = evt.newValue);

            var removeBtn = new Button(() => RemoveVisualLayer(layer)) { text = "X" };
            removeBtn.style.width = 24;
            removeBtn.style.height = 18;
            removeBtn.style.marginLeft = 5;
            removeBtn.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);

            header.Add(foldout);
            header.Add(removeBtn);
            container.Add(header);

            var nameField = new TextField("Name") { value = layer.LayerName };
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

            var worldLayerField = new ObjectField("Assigned World Layer")
            {
                objectType = typeof(WorldLayer),
                value = layer.AssignedWorldLayer
            };
            worldLayerField.RegisterValueChangedCallback(evt =>
            {
                layer.AssignedWorldLayer = evt.newValue as WorldLayer;
                EditorUtility.SetDirty(layer);
            });

            var heightField = new FloatField("Height") { value = layer.VisualHeight };
            heightField.RegisterValueChangedCallback(evt =>
            {
                layer.VisualHeight = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            var tilesetsLabel = new Label("Tilesets:");
            tilesetsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            tilesetsLabel.style.marginTop = 5;

            var tilesetsContainer = new VisualElement();
            RefreshTilesetsList(layer, tilesetsContainer);

            var addTilesetBtn = new Button(() =>
            {
                layer.AddTileset(null, 1f);
                EditorUtility.SetDirty(layer);
                serializedObject.ApplyModifiedProperties();
                RefreshTilesetsList(layer, tilesetsContainer);
            })
            {
                text = "+ Add Tileset"
            };

            foldout.Add(nameField);
            foldout.Add(enabledToggle);
            foldout.Add(worldLayerField);
            foldout.Add(heightField);
            foldout.Add(tilesetsLabel);
            foldout.Add(tilesetsContainer);
            foldout.Add(addTilesetBtn);

            return container;
        }

        private void RefreshTilesetsList(VisualLayer layer, VisualElement container)
        {
            container.Clear();

            for (var i = 0; i < layer.Tilesets.Count; i++)
            {
                var index = i;
                var weighted = layer.Tilesets[i];

                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 3;
                row.style.alignItems = Align.Center;

                var tilesetField = new ObjectField { objectType = typeof(Tileset), value = weighted.tileset };
                tilesetField.style.flexGrow = 1;
                tilesetField.RegisterValueChangedCallback(evt =>
                {
                    layer.Tilesets[index].tileset = evt.newValue as Tileset;
                    EditorUtility.SetDirty(layer);
                });

                var weightLabel = new Label("Weight:");
                weightLabel.style.marginLeft = 5;

                var weightField = new FloatField { value = weighted.weight };
                weightField.style.width = 50;
                weightField.RegisterValueChangedCallback(evt =>
                {
                    layer.Tilesets[index].weight = Mathf.Max(0.1f, evt.newValue);
                    EditorUtility.SetDirty(layer);
                });

                var removeBtn = new Button(() =>
                {
                    layer.Tilesets.RemoveAt(index);
                    EditorUtility.SetDirty(layer);
                    RefreshTilesetsList(layer, container);
                })
                {
                    text = "X"
                };
                removeBtn.style.width = 24;
                removeBtn.style.height = 18;
                removeBtn.style.marginLeft = 5;
                removeBtn.style.backgroundColor = new Color(0.6f, 0.2f, 0.2f);

                row.Add(tilesetField);
                row.Add(weightLabel);
                row.Add(weightField);
                row.Add(removeBtn);

                container.Add(row);
            }
        }

        private void AddNewWorldLayer()
        {
            var layer = CreateInstance<WorldLayer>();
            layer.LayerName = $"WorldLayer {gridManager.WorldLayerCollection.Layers.Count + 1}";

            var path = EditorUtility.SaveFilePanelInProject("Save World Layer", layer.LayerName, "asset", "");
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(layer, path);
            gridManager.WorldLayerCollection.AddLayer(layer);
            layer.InitializePreviewTexture(gridManager.Width, gridManager.Height);

            EditorUtility.SetDirty(gridManager);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            RefreshWorldLayers();
        }

        private void AddNewVisualLayer()
        {
            var layer = CreateInstance<VisualLayer>();
            layer.LayerName = $"VisualLayer {gridManager.VisualLayerCollection.Layers.Count + 1}";

            var path = EditorUtility.SaveFilePanelInProject("Save Visual Layer", layer.LayerName, "asset", "");
            if (string.IsNullOrEmpty(path)) return;

            AssetDatabase.CreateAsset(layer, path);
            gridManager.VisualLayerCollection.AddLayer(layer);

            EditorUtility.SetDirty(gridManager);
            serializedObject.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            RefreshVisualLayers();
        }

        private void RemoveWorldLayer(WorldLayer layer)
        {
            if (EditorUtility.DisplayDialog("Remove Layer",
                    $"Remove '{layer.LayerName}' from this GridManager?\n(The asset will not be deleted)", "Yes", "No"))
            {
                gridManager.WorldLayerCollection.RemoveLayer(layer);
                EditorUtility.SetDirty(gridManager);
                serializedObject.ApplyModifiedProperties();
                RefreshWorldLayers();
            }
        }

        private void RemoveVisualLayer(VisualLayer layer)
        {
            if (EditorUtility.DisplayDialog("Remove Layer",
                    $"Remove '{layer.LayerName}' from this GridManager?\n(The asset will not be deleted)", "Yes", "No"))
            {
                gridManager.VisualLayerCollection.RemoveLayer(layer);
                EditorUtility.SetDirty(gridManager);
                serializedObject.ApplyModifiedProperties();
                RefreshVisualLayers();
            }
        }

        private void ClearLayer(WorldLayer layer)
        {
            if (EditorUtility.DisplayDialog("Clear Layer", $"Clear all tiles from '{layer.LayerName}'?", "Yes", "No"))
            {
                layer.ClearPreviewTexture();
                EditorUtility.SetDirty(layer);

                if (gridManager != null)
                    ClearTilesFromLayerInManager(layer);

                SceneView.RepaintAll();
                RefreshWorldLayers();
            }
        }

        private void ClearTilesFromLayerInManager(WorldLayer layer)
        {
            if (gridManager.WorldGrid == null) return;

            var tilesToRemove = new List<Vector3Int>();

            foreach (var position in gridManager.WorldGrid.GetAllPositions())
            {
                var tile = gridManager.WorldGrid.GetTile(position.x, position.y, position.z);
                if (tile != null && tile.SourceLayer == layer)
                    tilesToRemove.Add(position);
            }

            foreach (var pos in tilesToRemove)
                gridManager.WorldGrid.RemoveTile(pos.x, pos.y, pos.z);

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

            var drawFluidsToggle = new Toggle("Draw Fluids")
            {
                value = serializedObject.FindProperty("drawFluids").boolValue
            };
            drawFluidsToggle.RegisterValueChangedCallback(evt =>
            {
                serializedObject.FindProperty("drawFluids").boolValue = evt.newValue;
                serializedObject.ApplyModifiedProperties();
            });

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

            debugFoldout.Add(drawGizmosToggle);
            debugFoldout.Add(drawWorldToggle);
            debugFoldout.Add(drawVisualToggle);
            debugFoldout.Add(drawFluidsToggle);
            debugFoldout.Add(worldGridColorPicker);
            debugFoldout.Add(visualGridColorPicker);

            root.Add(debugFoldout);
        }
    }
}