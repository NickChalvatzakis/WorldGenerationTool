using System;
using System.IO;
using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Data.SaveSystem;
using UnityEngine;

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
            // In Editor: Save to project folder
            return Path.Combine(Application.dataPath, "WorldSaves", fileName);
#else
            // At Runtime: Save to persistent data path
            return Path.Combine(Application.persistentDataPath, "WorldSaves", fileName);
#endif
        }

        /// <summary>
        /// Saves the current world to a file
        /// </summary>
        public static bool SaveWorld(GridManager gridManager, string worldName, SaveFormat format = SaveFormat.JSON)
        {
            try
            {
                // Create save data from current grid state
                var saveData = CreateSaveData(gridManager, worldName);

                // Ensure directory exists
                var savePath = GetSavePath(worldName, format);
                var directory = Path.GetDirectoryName(savePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                // Save based on format
                if (format == SaveFormat.JSON)
                {
                    var json = JsonUtility.ToJson(saveData, true);
                    File.WriteAllText(savePath, json);
                }
                else
                {
                    // Binary format (more compact)
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

        /// <summary>
        /// Loads a world from a file
        /// </summary>
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

        /// <summary>
        /// Creates save data from the current grid manager state
        /// </summary>
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

            // Save each layer's data
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

                    // Store only painted tiles (sparse storage)
                    if (layer.PreviewTexture != null)
                        for (var x = 0; x < layer.PreviewTexture.width; x++)
                        for (var y = 0; y < layer.PreviewTexture.height; y++)
                            if (layer.IsPixelPainted(x, y))
                                layerData.tiles.Add(new TileSaveData(x, y));

                    saveData.layers.Add(layerData);
                }

            return saveData;
        }

        /// <summary>
        /// Applies loaded save data to a grid manager
        /// </summary>
        public static void ApplySaveData(GridManager gridManager, WorldSaveData saveData)
        {
            if (gridManager == null || saveData == null)
            {
                Debug.LogError("[WorldSaveManager] Cannot apply save data - null reference");
                return;
            }

            Debug.Log($"[WorldSaveManager] Applying save data for world: {saveData.worldName}");

            // Note: Grid dimensions are set in GridManager inspector
            // You might want to validate or resize here
            if (saveData.gridSettings.width != gridManager.Width ||
                saveData.gridSettings.height != gridManager.Height)
                Debug.LogWarning($"[WorldSaveManager] Grid size mismatch! " +
                                 $"Save: {saveData.gridSettings.width}x{saveData.gridSettings.height}, " +
                                 $"Current: {gridManager.Width}x{gridManager.Height}");

            // Clear existing data
            gridManager.WorldGrid.SuppressEvents = true;
            gridManager.WorldGrid.Clear();

            // Find or create layers and paint tiles
            foreach (var layerData in saveData.layers)
            {
                // Try to find existing layer by GUID
                var layer = FindLayerByGuid(gridManager, layerData.layerGuid);

                // Or find by name
                if (layer == null)
                    layer = FindLayerByName(gridManager, layerData.layerName);

                if (layer == null)
                {
                    Debug.LogWarning(
                        $"[WorldSaveManager] Layer '{layerData.layerName}' not found in current scene. Skipping.");
                    continue;
                }

                // Clear layer first
                layer.ClearPreviewTexture();

                // Paint all saved tiles
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

        /// <summary>
        /// Gets all available save files
        /// </summary>
        public static string[] GetAvailableSaves(SaveFormat format = SaveFormat.JSON)
        {
            var extension = format == SaveFormat.JSON ? "*.json" : "*.dat";
            var savePath = GetSavePath("dummy", format);
            var directory = Path.GetDirectoryName(savePath);

            if (!Directory.Exists(directory))
                return new string[0];

            var files = Directory.GetFiles(directory, extension);

            // Extract just the world names
            for (var i = 0; i < files.Length; i++) files[i] = Path.GetFileNameWithoutExtension(files[i]);

            return files;
        }

        /// <summary>
        /// Deletes a save file
        /// </summary>
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