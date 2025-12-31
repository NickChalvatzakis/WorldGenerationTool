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

        public override VisualElement CreateInspectorGUI()
        {
            gridManager = target as GridManager;
            root = new VisualElement();

            CreateGridSettingsSection();
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

            // // Rebuild Visuals button
            // var rebuildButton = new Button(() => { gridManager.RebuildAllVisuals(); })
            // {
            //     text = "Rebuild All Visuals"
            // };
            // rebuildButton.style.marginTop = 10;
            // rebuildButton.style.height = 30;
            // root.Add(rebuildButton);
        }

        private void CreateLayerCollectionSection()
        {
            // World Layers
            worldLayersFoldout = new Foldout { text = "World Layers", value = true };
            worldLayersFoldout.style.marginTop = 10;

            var addWorldLayerBtn = new Button(() => AddNewWorldLayer())
            {
                text = "+ Add World Layer"
            };
            worldLayersFoldout.Add(addWorldLayerBtn);
            RefreshWorldLayers();
            root.Add(worldLayersFoldout);

            // Visual Layers
            visualLayersFoldout = new Foldout { text = "Visual Layers", value = true };
            visualLayersFoldout.style.marginTop = 10;

            var addVisualLayerBtn = new Button(() => AddNewVisualLayer())
            {
                text = "+ Add Visual Layer"
            };
            visualLayersFoldout.Add(addVisualLayerBtn);
            RefreshVisualLayers();
            root.Add(visualLayersFoldout);
        }

        private void RefreshWorldLayers()
        {
            while (worldLayersFoldout.childCount > 1) worldLayersFoldout.RemoveAt(1);

            if (gridManager.WorldLayerCollection?.Layers == null)
                return;

            foreach (var layer in gridManager.WorldLayerCollection.Layers)
            {
                var worldLayer = layer as WorldLayer;
                if (worldLayer != null) worldLayersFoldout.Add(CreateWorldLayerUI(worldLayer));
            }
        }

        private void RefreshVisualLayers()
        {
            while (visualLayersFoldout.childCount > 1) visualLayersFoldout.RemoveAt(1);

            if (gridManager.VisualLayerCollection?.Layers == null)
                return;

            foreach (var layer in gridManager.VisualLayerCollection.Layers)
            {
                var visualLayer = layer as VisualLayer;
                if (visualLayer != null) visualLayersFoldout.Add(CreateVisualLayerUI(visualLayer));
            }
        }

        private VisualElement CreateWorldLayerUI(WorldLayer layer)
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
            container.style.marginTop = 5;
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;

            var foldout = new Foldout { text = layer.LayerName, value = layer.foldoutState };
            foldout.style.flexGrow = 1;
            foldout.RegisterValueChangedCallback(evt => layer.foldoutState = evt.newValue);

            var removeBtn = new Button(() => RemoveWorldLayer(layer)) { text = "X" };
            removeBtn.style.width = 30;

            header.Add(foldout);
            header.Add(removeBtn);
            container.Add(header);

            // Properties
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

            foldout.Add(nameField);
            foldout.Add(enabledToggle);
            foldout.Add(lockToggle);
            foldout.Add(colorField);
            foldout.Add(clearBtn);

            return container;
        }

        private VisualElement CreateVisualLayerUI(VisualLayer layer)
        {
            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.2f, 0.2f, 0.3f, 0.3f);
            container.style.marginTop = 5;
            container.style.borderBottomLeftRadius = 4;
            container.style.borderBottomRightRadius = 4;
            container.style.borderTopLeftRadius = 4;
            container.style.borderTopRightRadius = 4;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;

            var foldout = new Foldout { text = layer.LayerName, value = layer.foldoutState };
            foldout.style.flexGrow = 1;
            foldout.RegisterValueChangedCallback(evt => layer.foldoutState = evt.newValue);

            var removeBtn = new Button(() => RemoveVisualLayer(layer)) { text = "X" };
            removeBtn.style.width = 30;

            header.Add(foldout);
            header.Add(removeBtn);
            container.Add(header);

            // Properties
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

            var heightField = new IntegerField("Height") { value = layer.DefaultLayerHeight };
            heightField.RegisterValueChangedCallback(evt =>
            {
                layer.DefaultLayerHeight = evt.newValue;
                EditorUtility.SetDirty(layer);
            });

            // Tilesets
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

                var tilesetField = new ObjectField { objectType = typeof(Tileset), value = weighted.tileset };
                tilesetField.style.flexGrow = 1;
                tilesetField.RegisterValueChangedCallback(evt =>
                {
                    layer.Tilesets[index].tileset = evt.newValue as Tileset;
                    EditorUtility.SetDirty(layer);
                });

                var weightField = new FloatField { value = weighted.weight };
                weightField.style.width = 60;
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
                removeBtn.style.width = 25;

                row.Add(tilesetField);
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
            if (EditorUtility.DisplayDialog("Remove Layer", $"Remove '{layer.LayerName}'?", "Yes", "No"))
            {
                gridManager.WorldLayerCollection.RemoveLayer(layer);
                EditorUtility.SetDirty(gridManager);
                serializedObject.ApplyModifiedProperties();
                RefreshWorldLayers();
            }
        }

        private void RemoveVisualLayer(VisualLayer layer)
        {
            if (EditorUtility.DisplayDialog("Remove Layer", $"Remove '{layer.LayerName}'?", "Yes", "No"))
            {
                gridManager.VisualLayerCollection.RemoveLayer(layer);
                EditorUtility.SetDirty(gridManager);
                serializedObject.ApplyModifiedProperties();
                RefreshVisualLayers();
            }
        }

        private void ClearLayer(WorldLayer layer)
        {
            if (EditorUtility.DisplayDialog("Clear Layer", $"Clear '{layer.LayerName}'?", "Yes", "No"))
            {
                layer.ClearPreviewTexture();
                EditorUtility.SetDirty(layer);

                if (gridManager != null) ClearTilesFromLayerInManager(layer);

                SceneView.RepaintAll();
            }
        }

        private void ClearTilesFromLayerInManager(WorldLayer layer)
        {
            if (gridManager.WorldGrid == null) return;

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
            debugFoldout.Add(worldGridColorPicker);
            debugFoldout.Add(visualGridColorPicker);

            root.Add(debugFoldout);
        }
    }
}