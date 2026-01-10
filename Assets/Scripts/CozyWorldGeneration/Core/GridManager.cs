using System.Collections.Generic;
using CozyWorldGeneration.Core.DualGrid;
using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Core.SaveSystem;
using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Events;
using UnityEngine;

namespace CozyWorldGeneration.Core
{
    [ExecuteAlways]
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")] [SerializeField]
        private int gridWidth = 20;

        [SerializeField] private int gridHeight = 20;
        [SerializeField] private int gridMaxLevels = 10;
        [SerializeField] private float tileSize = 1f;
        private Dictionary<WorldLayer, VisualGrid> visualGrids = new();

        [Header("Layer Collections")] [SerializeField]
        private WorldLayerCollection worldLayerCollection;

        [SerializeField] private VisualLayerCollection visualLayerCollection;

        [Header("Save/Load")] [SerializeField] private string worldName = "MyWorld";
        [SerializeField] private bool autoLoadOnStart = false;
        [SerializeField] private WorldSaveManager.SaveFormat saveFormat = WorldSaveManager.SaveFormat.JSON;


        [Header("Debug")] [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawWorldGrid = true;
        [SerializeField] private bool drawVisualGrid = true;
        [SerializeField] private Color worldGridColor = new(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color visualGridColor = new(0.2f, 0.2f, 0.2f);
        // [SerializeField] private bool drawDebugTiles = false;

        public WorldGrid WorldGrid { get; private set; }

        public int Width => gridWidth;
        public int Height => gridHeight;
        public int MaxLevels => gridMaxLevels;
        public float TileSize => tileSize;

        public WorldLayerCollection WorldLayerCollection => worldLayerCollection;
        public VisualLayerCollection VisualLayerCollection => visualLayerCollection;

        private Transform visualTilesContainer;

        // public Transform VisualTilesContainer => visualTilesContainer;
        //
        // public bool DrawDebugTiles => drawDebugTiles;

        private void Awake()
        {
            InitializeGrids();
            RebuildFromLayerData();
        }

        private void Start()
        {
            if (autoLoadOnStart && !string.IsNullOrEmpty(worldName)) LoadWorld(worldName);
        }

        public void LoadWorld(string saveName)
        {
            var saveData = WorldSaveManager.LoadWorld(saveName, saveFormat);

            if (saveData != null)
            {
                WorldSaveManager.ApplySaveData(this, saveData);
                worldName = saveName;
            }
        }


        [ContextMenu("Load World")]
        public void LoadWorld()
        {
            LoadWorld(worldName);
        }


        [ContextMenu("Save World")]
        public void SaveWorld()
        {
            if (string.IsNullOrEmpty(worldName))
            {
                Debug.LogError("[GridManager] World name is empty!");
                return;
            }

            WorldSaveManager.SaveWorld(this, worldName, saveFormat);
        }

        public void SaveWorldAs(string saveName)
        {
            worldName = saveName;
            SaveWorld();
        }

        public string[] GetAvailableSaves()
        {
            return WorldSaveManager.GetAvailableSaves(saveFormat);
        }


        private void OnEnable()
        {
            SubscribeToEvents();
#if UNITY_EDITOR
            RebuildFromLayerData();
#endif
        }

        public void RebuildFromLayerData()
        {
            if (WorldGrid == null || worldLayerCollection == null) return;

            WorldGrid.SuppressEvents = true;
            WorldGrid.Clear();

            foreach (var layer in worldLayerCollection.Layers)
            {
                if (layer == null) continue;

                layer.ForceRebuildTexture(gridWidth, gridHeight);

                if (layer.PreviewTexture == null) continue;

                for (var x = 0; x < layer.PreviewTexture.width; x++)
                for (var y = 0; y < layer.PreviewTexture.height; y++)
                    if (layer.IsPixelPainted(x, y))
                        WorldGrid.PlaceTile(x, y, layer);
            }

            WorldGrid.SuppressEvents = false;
            RefreshAllVisualGrids();

            Debug.Log($"[GridManager] Rebuilt grid from layer data.");
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // if (WorldGrid == null) InitializeGrids();
        }

        private void Reset()
        {
            InitializeGrids();
        }
#endif

        public void InitializeGrids()
        {
            if (worldLayerCollection == null) worldLayerCollection = new WorldLayerCollection("World Layers");
            if (visualLayerCollection == null) visualLayerCollection = new VisualLayerCollection("Visual Layers");

            if (visualTilesContainer == null)
            {
                var container = new GameObject("Visual Tiles");
                container.transform.SetParent(transform);
                container.hideFlags = HideFlags.DontSave;
                visualTilesContainer = container.transform;
            }

            WorldGrid = new WorldGrid(gridWidth, gridHeight, gridMaxLevels);

            foreach (var layer in worldLayerCollection.Layers)
                if (layer != null)
                    CreateVisualGridForLayer(layer);


            InitializeLayerTextures();
            ToolEvents.TriggerGridInitialized(gridWidth, gridHeight);

            Debug.Log(
                $"GridManager initialized: WorldGrid ({gridWidth}x{gridHeight}), VisualGrid ({gridWidth - 1}x{gridHeight - 1})");
        }

        private void CreateVisualGridForLayer(WorldLayer layer)
        {
            if (visualGrids.ContainsKey(layer)) return;

            var visualGrid = new VisualGrid(gridWidth - 1, gridHeight - 1, WorldGrid, layer, TileSize);
            var container = new GameObject($"Layer_{layer.LayerName}");
            container.transform.SetParent(visualTilesContainer);
            container.hideFlags = HideFlags.DontSave;

            // Auto-offset based on layer index
            var layerIndex = worldLayerCollection.Layers.IndexOf(layer);
            container.transform.localPosition = new Vector3(0, layerIndex * -0.05f, 0);

            visualGrid.TilesContainer = container.transform;

            visualGrid.GetVisualLayerForWorldLayer = (worldLayer) =>
            {
                return visualLayerCollection?.GetVisualLayerForWorldLayer(worldLayer);
            };

            visualGrids[layer] = visualGrid;
        }


        private void InitializeLayerTextures()
        {
            foreach (var layer in worldLayerCollection.Layers)
                if (layer.PreviewTexture == null ||
                    layer.PreviewTexture.width != gridWidth ||
                    layer.PreviewTexture.height != gridHeight)
                    layer.InitializePreviewTexture(gridWidth, gridHeight);
        }

        // TODO: add all the actions in events not just layer actions.
        private void SubscribeToEvents()
        {
            ToolEvents.OnLayerAdded += HandleLayerAdded;
            ToolEvents.OnLayerRemoved += HandleLayerRemoved;
            ToolEvents.OnLayerCleared += HandleLayerCleared;
            ToolEvents.OnTileChanged += HandleTileChanged;
        }

        private void UnsubscribeFromEvents()
        {
            ToolEvents.OnLayerAdded -= HandleLayerAdded;
            ToolEvents.OnLayerRemoved -= HandleLayerRemoved;
            ToolEvents.OnLayerCleared -= HandleLayerCleared;
            ToolEvents.OnTileChanged -= HandleTileChanged;
        }

        private void HandleTileChanged(int x, int y)
        {
            Debug.Log($"[GridManager] HandleTileChanged({x}, {y}) - visualGrids count: {visualGrids.Count}");

            var affectedPositions = new Vector2Int[]
            {
                new(x, y),
                new(x - 1, y),
                new(x, y - 1),
                new(x - 1, y - 1)
            };

            foreach (var visualGrid in visualGrids.Values)
            foreach (var pos in affectedPositions)
                visualGrid.UpdateVisualTile(pos.x, pos.y);
        }

        private void HandleLayerAdded(WorldLayer layer)
        {
            if (layer != null)
            {
                layer.InitializePreviewTexture(gridWidth, gridHeight);
                CreateVisualGridForLayer(layer);
            }
        }

        private void HandleLayerRemoved(WorldLayer layer)
        {
            if (layer != null) ClearTilesFromLayer(layer);
        }

        private void HandleLayerCleared(WorldLayer layer)
        {
            ClearTilesFromLayer(layer);
        }

        private void ClearTilesFromLayer(WorldLayer layer)
        {
            if (WorldGrid == null) return;

            var tilesToRemove = new List<Vector3Int>();

            foreach (var position in WorldGrid.GetAllPositions())
            {
                var tile = WorldGrid.GetTile(position.x, position.y, position.z);
                if (tile != null && tile.SourceLayer == layer)
                    tilesToRemove.Add(position);
            }

            foreach (var pos in tilesToRemove)
                WorldGrid.RemoveTile(pos.x, pos.y, pos.z);

#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        public void RemoveTile(int x, int y, int level)
        {
            WorldGrid?.RemoveTile(x, y, level);
        }

        public WorldTile GetWorldTile(int x, int y, int level)
        {
            return WorldGrid?.GetTile(x, y, level);
        }


        public void ClearGrids()
        {
            foreach (var visualGrid in visualGrids.Values)
                visualGrid?.Clear();
            visualGrids.Clear();
            WorldGrid?.Clear();
            Debug.Log("Grids cleared");
        }

        public Vector3 GridToWorldPosition(int x, int y)
        {
            return new Vector3((x + 0.5f) * tileSize, 0f, (y + 0.5f) * tileSize);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            var x = Mathf.FloorToInt(worldPos.x / tileSize);
            var y = Mathf.FloorToInt(worldPos.z / tileSize);
            return new Vector2Int(x, y);
        }

        //TODO: rebuild world grid. and that will rebuild the visuals

        // /// <summary>
        // /// Rebuilds all visual tiles from the current WorldGrid state.
        // /// </summary>
        // [ContextMenu("Rebuild All Visuals")]
        // public void RebuildAllVisuals()
        // {
        //     if (VisualGrid == null)
        //     {
        //         Debug.LogWarning("VisualGrid is null, cannot rebuild visuals");
        //         return;
        //     }
        //
        //     Debug.Log("Rebuilding all visual tiles...");
        //     VisualGrid.UpdateAllVisuals();
        //     Debug.Log("Visual rebuild complete");
        // }

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!drawGizmos || WorldGrid == null)
                return;

            if (drawWorldGrid) DrawWorldGridGizmos();

            if (drawVisualGrid) DrawVisualGridGizmos();
        }

        private void DrawWorldGridGizmos()
        {
            Gizmos.color = worldGridColor;

            for (var y = 0; y <= gridHeight; y++)
            {
                var start = new Vector3(0, 0, y * tileSize);
                var end = new Vector3(gridWidth * tileSize, 0, y * tileSize);
                Gizmos.DrawLine(start, end);
            }

            for (var x = 0; x <= gridWidth; x++)
            {
                var start = new Vector3(x * tileSize, 0, 0);
                var end = new Vector3(x * tileSize, 0, gridHeight * tileSize);
                Gizmos.DrawLine(start, end);
            }

            foreach (var position in WorldGrid.GetAllPositions())
            {
                var tile = WorldGrid.GetTile(position.x, position.y, position.z);
                if (tile != null)
                {
                    var x = position.x;
                    var y = position.y;
                    var level = position.z;

                    var tileColor = tile.SourceLayer != null ? tile.SourceLayer.LayerColor : Color.white;
                    tileColor.a = 0.3f;
                    Gizmos.color = tileColor;

                    // Offset gizmo Y based on level for visibility
                    var yOffset = 0.01f + level * 0.02f;

                    var corners = new Vector3[4]
                    {
                        new(x * tileSize, yOffset, y * tileSize),
                        new((x + 1) * tileSize, yOffset, y * tileSize),
                        new((x + 1) * tileSize, yOffset, (y + 1) * tileSize),
                        new(x * tileSize, yOffset, (y + 1) * tileSize)
                    };

                    Gizmos.DrawLine(corners[0], corners[1]);
                    Gizmos.DrawLine(corners[1], corners[2]);
                    Gizmos.DrawLine(corners[2], corners[3]);
                    Gizmos.DrawLine(corners[3], corners[0]);
                    Gizmos.DrawLine(corners[0], corners[2]);
                    Gizmos.DrawLine(corners[1], corners[3]);
                }
            }
        }

        private void DrawVisualGridGizmos()
        {
            Gizmos.color = visualGridColor;

            var offsetX = tileSize * 0.5f;
            var offsetZ = tileSize * 0.5f;

            for (var y = 0; y <= gridHeight - 1; y++)
            {
                var start = new Vector3(offsetX, 0.02f, y * tileSize + offsetZ);
                var end = new Vector3((gridWidth - 1) * tileSize + offsetX, 0.02f, y * tileSize + offsetZ);
                Gizmos.DrawLine(start, end);
            }

            for (var x = 0; x <= gridWidth - 1; x++)
            {
                var start = new Vector3(x * tileSize + offsetX, 0.02f, offsetZ);
                var end = new Vector3(x * tileSize + offsetX, 0.02f, (gridHeight - 1) * tileSize + offsetZ);
                Gizmos.DrawLine(start, end);
            }

            for (var x = 0; x < gridWidth - 1; x++)
            for (var y = 0; y < gridHeight - 1; y++)
                foreach (var visualGrid in visualGrids.Values)
                {
                    var tile = visualGrid.GetTile(x, y);
                    if (tile != null && tile.ConfigurationIndex > 0)
                    {
                        var pos = GridToWorldPosition(x, y) + new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f);
                        pos.y = 0.02f;

                        Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);

                        var crossSize = tileSize * 0.1f;
                        Gizmos.DrawLine(pos - Vector3.right * crossSize, pos + Vector3.right * crossSize);
                        Gizmos.DrawLine(pos - Vector3.forward * crossSize, pos + Vector3.forward * crossSize);
                        break;
                    }
                }
        }

        #endregion

#if UNITY_EDITOR
        [ContextMenu("Reinitialize Grids")]
        public void EditorReinitialize()
        {
            if (worldLayerCollection?.Layers != null)
                foreach (var layer in worldLayerCollection.Layers)
                    if (layer != null)
                    {
                        layer.ClearPreviewTexture();
                        UnityEditor.EditorUtility.SetDirty(layer);
                    }

            ClearGrids();
            InitializeGrids();
        }
#endif
        public void RefreshAllVisualGrids()
        {
            if (visualGrids == null)
            {
                Debug.LogWarning("[GridManager] visualGrids is null!");
                return;
            }

            Debug.Log($"[GridManager] RefreshAllVisualGrids - visualGrids count: {visualGrids.Count}");

            foreach (var kvp in visualGrids)
            {
                var layer = kvp.Key;
                var visualGrid = kvp.Value;

                Debug.Log(
                    $"[GridManager] Refreshing layer: {layer.LayerName}, TilesContainer: {visualGrid.TilesContainer != null}");

                var tilesUpdated = 0;
                for (var x = 0; x < visualGrid.Width; x++)
                for (var y = 0; y < visualGrid.Height; y++)
                {
                    visualGrid.UpdateVisualTile(x, y);
                    var tile = visualGrid.GetTile(x, y);
                    if (tile != null && tile.ConfigurationIndex > 0)
                        tilesUpdated++;
                }

                Debug.Log($"[GridManager] Layer {layer.LayerName} - tiles with config > 0: {tilesUpdated}");
            }
        }

        public void RefreshVisualGridForLayer(WorldLayer layer)
        {
            if (layer == null || !visualGrids.ContainsKey(layer)) return;

            var visualGrid = visualGrids[layer];

            for (var x = 0; x < visualGrid.Width; x++)
            for (var y = 0; y < visualGrid.Height; y++)
                visualGrid.UpdateVisualTile(x, y);

            Debug.Log($"[GridManager] Refreshed visual grid for layer: {layer.LayerName}");
        }
    }
}