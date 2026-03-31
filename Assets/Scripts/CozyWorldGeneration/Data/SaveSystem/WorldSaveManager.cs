using System;
using System.IO;
using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Data.SaveSystem;
using CozyWorldGeneration.Data.Tilesets;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CozyWorldGeneration.Core.SaveSystem
{
    public class WorldSaveManager
    {
        public enum SaveFormat
        {
            JSON,
            Binary
        }

        private static string GetSavePath(string worldName, SaveFormat format)
        {
            var extension = format == SaveFormat.JSON ? ".json" : ".dat";
            var fileName = $"{worldName}{extension}";

#if UNITY_EDITOR
            return Path.Combine(Application.dataPath, "WorldSaves", fileName);
#else
            return Path.Combine(Application.persistentDataPath, "WorldSaves", fileName);
#endif
        }

        public static bool SaveWorld(GridManager gridManager, string worldName, SaveFormat format = SaveFormat.JSON)
        {
            try
            {
                var saveData = CreateSaveData(gridManager, worldName);

                var savePath = GetSavePath(worldName, format);
                var directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                if (format == SaveFormat.JSON)
                {
                    var json = JsonUtility.ToJson(saveData, true);
                    File.WriteAllText(savePath, json);
                }
                else
                {
                    var json = JsonUtility.ToJson(saveData);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                    File.WriteAllBytes(savePath, bytes);
                }

                Debug.Log($"[WorldSaveManager] Saved world '{worldName}' to: {savePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSaveManager] Failed to save world: {e.Message}");
                return false;
            }
        }

        public static WorldSaveData LoadWorld(string worldName, SaveFormat format = SaveFormat.JSON)
        {
            try
            {
                var savePath = GetSavePath(worldName, format);

                if (!File.Exists(savePath))
                {
                    Debug.LogWarning($"[WorldSaveManager] Save file not found: {savePath}");
                    return null;
                }

                string json;

                if (format == SaveFormat.JSON)
                {
                    json = File.ReadAllText(savePath);
                }
                else
                {
                    var bytes = File.ReadAllBytes(savePath);
                    json = System.Text.Encoding.UTF8.GetString(bytes);
                }

                var saveData = JsonUtility.FromJson<WorldSaveData>(json);
                Debug.Log($"[WorldSaveManager] Loaded world '{worldName}' from: {savePath}");
                return saveData;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSaveManager] Failed to load world: {e.Message}");
                return null;
            }
        }

        private static WorldSaveData CreateSaveData(GridManager gridManager, string worldName)
        {
            var saveData = new WorldSaveData
            {
                worldName = worldName,
                author = SystemInfo.deviceName,
                gridSettings = new GridSettings(
                    gridManager.Width,
                    gridManager.Height,
                    gridManager.MaxLevels,
                    gridManager.TileSize
                )
            };

            if (gridManager.WorldLayerCollection?.Layers != null)
                foreach (var layer in gridManager.WorldLayerCollection.Layers)
                {
                    if (layer == null) continue;

                    var layerData = new LayerSaveData(
                        layer.LayerName,
                        layer.LayerLevel,
                        layer.LayerColor
                    );

                    layerData.layerGuid = layer.GUID;
                    layerData.isEnabled = layer.IsEnabled;

#if UNITY_EDITOR
                    // Store asset path for editor reloading
                    layerData.assetPath = AssetDatabase.GetAssetPath(layer);
#endif

                    if (layer.PreviewTexture != null)
                        for (var x = 0; x < layer.PreviewTexture.width; x++)
                        for (var y = 0; y < layer.PreviewTexture.height; y++)
                            if (layer.IsPixelPainted(x, y))
                                layerData.tiles.Add(new TileSaveData(x, y));

                    saveData.layers.Add(layerData);
                }

            if (gridManager.VisualLayerCollection?.Layers != null)
                foreach (var visualLayer in gridManager.VisualLayerCollection.Layers)
                {
                    if (visualLayer == null) continue;

                    var visualLayerData = new VisualLayerSaveData
                    {
                        layerName = visualLayer.LayerName,
                        layerGuid = visualLayer.GUID,
                        isEnabled = visualLayer.IsEnabled,
                        isFluidLayer = visualLayer.IsFluidLayer,
                        visualHeight = visualLayer.VisualHeight,
                        assignedWorldLayerGuid = visualLayer.AssignedWorldLayer?.GUID
                    };

#if UNITY_EDITOR
                    visualLayerData.assetPath = AssetDatabase.GetAssetPath(visualLayer);

                    // Save tileset references
                    foreach (var weightedTileset in visualLayer.Tilesets)
                        if (weightedTileset.tileset != null)
                        {
                            var tilesetPath = AssetDatabase.GetAssetPath(weightedTileset.tileset);
                            visualLayerData.tilesetReferences.Add(new TilesetReference(tilesetPath,
                                weightedTileset.weight));
                        }
#endif

                    saveData.visualLayers.Add(visualLayerData);
                }

            return saveData;
        }

        public static void ApplySaveData(GridManager gridManager, WorldSaveData saveData)
        {
            if (gridManager == null || saveData == null)
            {
                Debug.LogError("[WorldSaveManager] Cannot apply save data - null reference");
                return;
            }

            Debug.Log($"[WorldSaveManager] Applying save data for world: {saveData.worldName}");

            if (saveData.gridSettings.width != gridManager.Width ||
                saveData.gridSettings.height != gridManager.Height)
                Debug.LogWarning($"[WorldSaveManager] Grid size mismatch! " +
                                 $"Save: {saveData.gridSettings.width}x{saveData.gridSettings.height}, " +
                                 $"Current: {gridManager.Width}x{gridManager.Height}");

            // Apply VisualLayers FIRST (so they're available when we refresh)
            ApplyVisualLayers(gridManager, saveData);

            gridManager.WorldGrid.SuppressEvents = true;
            gridManager.WorldGrid.Clear();

            foreach (var layerData in saveData.layers)
            {
                var layer = FindLayerByGuid(gridManager, layerData.layerGuid);

                if (layer == null)
                    layer = FindLayerByName(gridManager, layerData.layerName);

#if UNITY_EDITOR
                // Try to load from asset path if not found
                if (layer == null && !string.IsNullOrEmpty(layerData.assetPath))
                {
                    layer = AssetDatabase.LoadAssetAtPath<WorldLayer>(layerData.assetPath);
                    if (layer != null)
                    {
                        gridManager.AddLayerToCollection(layer);
                        Debug.Log($"[WorldSaveManager] Loaded WorldLayer from asset: {layerData.assetPath}");
                    }
                }
#endif

                if (layer == null)
                {
                    Debug.LogWarning($"[WorldSaveManager] Layer '{layerData.layerName}' not found. Skipping.");
                    continue;
                }

                layer.ClearPreviewTexture();

                foreach (var tileData in layerData.tiles)
                {
                    layer.PaintPixel(tileData.x, tileData.y, true);
                    gridManager.WorldGrid.PlaceTile(tileData.x, tileData.y, layer);
                }

                Debug.Log(
                    $"[WorldSaveManager] Loaded layer '{layerData.layerName}' with {layerData.tiles.Count} tiles");
            }

            gridManager.WorldGrid.SuppressEvents = false;
            gridManager.RefreshAllVisualGrids();

            Debug.Log($"[WorldSaveManager] Successfully applied save data");
        }

        private static void ApplyVisualLayers(GridManager gridManager, WorldSaveData saveData)
        {
            if (saveData.visualLayers == null || saveData.visualLayers.Count == 0)
            {
                Debug.LogWarning("[WorldSaveManager] No visual layers in save data");
                return;
            }

#if UNITY_EDITOR
            if (gridManager.VisualLayerCollection == null)
            {
                Debug.LogWarning("[WorldSaveManager] VisualLayerCollection is null - cannot load visual layers");
                return;
            }

            foreach (var visualLayerData in saveData.visualLayers)
            {
                VisualLayer visualLayer = null;

                // Try to find existing layer by GUID
                foreach (var existing in gridManager.VisualLayerCollection.Layers)
                    if (existing != null && existing.GUID == visualLayerData.layerGuid)
                    {
                        visualLayer = existing;
                        break;
                    }

                // Try to load from asset path
                if (visualLayer == null && !string.IsNullOrEmpty(visualLayerData.assetPath))
                {
                    visualLayer = AssetDatabase.LoadAssetAtPath<VisualLayer>(visualLayerData.assetPath);

                    if (visualLayer != null && !gridManager.VisualLayerCollection.Layers.Contains(visualLayer))
                    {
                        gridManager.VisualLayerCollection.AddLayer(visualLayer);
                        Debug.Log($"[WorldSaveManager] Added VisualLayer from asset: {visualLayerData.assetPath}");
                    }
                }

                if (visualLayer == null)
                {
                    Debug.LogWarning(
                        $"[WorldSaveManager] VisualLayer '{visualLayerData.layerName}' not found at path: {visualLayerData.assetPath}");
                    continue;
                }

                visualLayer.IsEnabled = visualLayerData.isEnabled;
                visualLayer.IsFluidLayer = visualLayerData.isFluidLayer;
                visualLayer.VisualHeight = visualLayerData.visualHeight;

                // Find and assign the WorldLayer reference
                if (!string.IsNullOrEmpty(visualLayerData.assignedWorldLayerGuid))
                {
                    var worldLayer = FindLayerByGuid(gridManager, visualLayerData.assignedWorldLayerGuid);
                    if (worldLayer != null)
                    {
                        visualLayer.AssignedWorldLayer = worldLayer;
                        Debug.Log(
                            $"[WorldSaveManager] Linked VisualLayer '{visualLayer.LayerName}' -> WorldLayer '{worldLayer.LayerName}'");
                    }
                }

                // Restore tileset references
                if (visualLayerData.tilesetReferences != null && visualLayerData.tilesetReferences.Count > 0)
                {
                    visualLayer.Tilesets.Clear();
                    foreach (var tilesetRef in visualLayerData.tilesetReferences)
                    {
                        if (string.IsNullOrEmpty(tilesetRef.assetPath)) continue;

                        var tileset = AssetDatabase.LoadAssetAtPath<Tileset>(tilesetRef.assetPath);
                        if (tileset != null)
                            visualLayer.AddTileset(tileset, tilesetRef.weight);
                        else
                            Debug.LogWarning($"[WorldSaveManager] Tileset not found: {tilesetRef.assetPath}");
                    }
                }

                EditorUtility.SetDirty(visualLayer);
            }

            Debug.Log($"[WorldSaveManager] Applied {saveData.visualLayers.Count} visual layers");
#else
            Debug.LogWarning("[WorldSaveManager] Visual layer loading from asset paths only works in Editor");
#endif
        }

        private static WorldLayer FindLayerByGuid(GridManager gridManager, string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;

            foreach (var layer in gridManager.WorldLayerCollection.Layers)
                if (layer != null && layer.GUID == guid)
                    return layer;
            return null;
        }

        private static WorldLayer FindLayerByName(GridManager gridManager, string name)
        {
            foreach (var layer in gridManager.WorldLayerCollection.Layers)
                if (layer != null && layer.LayerName == name)
                    return layer;
            return null;
        }

        public static string[] GetAvailableSaves(SaveFormat format = SaveFormat.JSON)
        {
            var extension = format == SaveFormat.JSON ? "*.json" : "*.dat";
            var savePath = GetSavePath("dummy", format);
            var directory = Path.GetDirectoryName(savePath);

            if (!Directory.Exists(directory))
                return new string[0];

            var files = Directory.GetFiles(directory, extension);

            for (var i = 0; i < files.Length; i++)
                files[i] = Path.GetFileNameWithoutExtension(files[i]);

            return files;
        }

        public static bool DeleteWorld(string worldName, SaveFormat format = SaveFormat.JSON)
        {
            try
            {
                var savePath = GetSavePath(worldName, format);
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                    Debug.Log($"[WorldSaveManager] Deleted world: {worldName}");
                    return true;
                }

                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[WorldSaveManager] Failed to delete world: {e.Message}");
                return false;
            }
        }
    }
}