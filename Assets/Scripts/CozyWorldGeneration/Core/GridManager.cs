using System.Collections.Generic;
using CozyWorldGeneration.Core.DualGrid;
using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Core.Events;
using CozyWorldGeneration.Core.Fluids;
using CozyWorldGeneration.Core.SaveSystem;
using CozyWorldGeneration.Data.Layers;
using UnityEngine;

namespace CozyWorldGeneration.Core
{
    /// <summary>
    /// Orchestrates world grid data, visual grids, fluid simulation wiring, and save/load hooks.
    /// </summary>
    [ExecuteAlways]
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")] [SerializeField]
        private int gridWidth = 20;

        [SerializeField] private int gridHeight = 20;
        [SerializeField] private int gridMaxLevels = 10;
        [SerializeField] private float tileSize = 1f;

        [Header("Fluid Settings")] [SerializeField]
        private bool enableFluids = true;

        [SerializeField] private float fluidVisualHeightOffset = -0.03f;

        private Dictionary<WorldLayer, VisualGrid> visualGrids = new();
        private VisualGrid fluidVisualGrid;

        [Header("Layer Collections")] [SerializeField]
        private WorldLayerCollection worldLayerCollection;

        [SerializeField] private VisualLayerCollection visualLayerCollection;

        [Header("Save/Load")] [SerializeField] private string worldName = "MyWorld";
        [SerializeField] private bool autoLoadOnStart = false;
        [SerializeField] private WorldSaveManager.SaveFormat saveFormat = WorldSaveManager.SaveFormat.JSON;

        [Header("Debug")] [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawWorldGrid = true;
        [SerializeField] private bool drawVisualGrid = true;
        [SerializeField] private bool drawFluidGrid = true;
        [SerializeField] private Color worldGridColor = new(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color visualGridColor = new(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color fluidGridColor = new(0.2f, 0.5f, 1f);

        public WorldGrid WorldGrid { get; private set; }
        public FluidSimulator FluidSimulator { get; private set; }

        public int Width => gridWidth;
        public int Height => gridHeight;
        public int MaxLevels => gridMaxLevels;
        public float TileSize => tileSize;

        public WorldLayerCollection WorldLayerCollection => worldLayerCollection;
        public VisualLayerCollection VisualLayerCollection => visualLayerCollection;

        private Transform visualTilesContainer;

        private void Awake()
        {
            InitializeGrids();
            RebuildFromLayerData();

            if (enableFluids) InitializeFluids();
        }

        private void InitializeFluids()
        {
            FluidSimulator = GetComponent<FluidSimulator>();
            if (FluidSimulator != null)
                FluidSimulator.Initialize(this);
            else
                Debug.LogWarning("[GridManager] FluidSimulator component not found!");
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
                RefreshAllVisualGrids();

                Debug.Log($"[GridManager] Loaded world '{saveName}' and refreshed visuals");
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
        }

        private void Reset()
        {
            InitializeGrids();
        }
#endif

        /// <summary>
        /// Initializes world/visual grids and wires layer-driven visual rendering.
        /// </summary>
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
            if (enableFluids)
                CreateFluidVisualGrid();

            InitializeLayerTextures();
            ToolEvents.TriggerGridInitialized(gridWidth, gridHeight);

            Debug.Log(
                $"GridManager initialized: WorldGrid ({gridWidth}x{gridHeight}), VisualGrids ({gridWidth - 1}x{gridHeight - 1})");
        }

        private void CreateVisualGridForLayer(WorldLayer layer)
        {
            if (visualGrids.ContainsKey(layer)) return;

            var visualGrid = new VisualGrid(gridWidth - 1, gridHeight - 1, WorldGrid, layer, TileSize);
            var container = new GameObject($"Layer_{layer.LayerName}");
            container.transform.SetParent(visualTilesContainer);
            container.hideFlags = HideFlags.DontSave;

            var layerIndex = worldLayerCollection.Layers.IndexOf(layer);
            container.transform.localPosition = new Vector3(0, layerIndex * -0.05f, 0);

            visualGrid.TilesContainer = container.transform;

            visualGrid.GetVisualLayerForWorldLayer = (worldLayer) =>
            {
                return visualLayerCollection?.GetVisualLayerForWorldLayer(worldLayer);
            };

            visualGrids[layer] = visualGrid;

            Debug.Log($"[GridManager] Created visual grid for layer: {layer.LayerName}");
        }

        /// <summary>
        /// Creates the dedicated visual grid that renders fluid occupancy.
        /// </summary>
        private void CreateFluidVisualGrid()
        {
            if (visualLayerCollection == null)
            {
                Debug.LogWarning("[GridManager] Cannot create fluid visual grid - no visual layer collection!");
                return;
            }

            var fluidVisualLayer = visualLayerCollection.GetFluidVisualLayer();
            if (fluidVisualLayer == null)
            {
                Debug.LogWarning(
                    "[GridManager] No fluid visual layer found! Create a VisualLayer and mark it as IsFluidLayer.");
                return;
            }

            var fluidWorldLayer = ScriptableObject.CreateInstance<WorldLayer>();
            fluidWorldLayer.LayerLevel = 0;
            fluidWorldLayer.LayerName = "Fluid_Internal";

            fluidVisualGrid = new VisualGrid(gridWidth - 1, gridHeight - 1, WorldGrid, fluidWorldLayer, TileSize, true);

            var container = new GameObject("Fluid_Visuals");
            container.transform.SetParent(visualTilesContainer);
            container.hideFlags = HideFlags.DontSave;
            container.transform.localPosition = new Vector3(0, fluidVisualHeightOffset, 0);

            fluidVisualGrid.TilesContainer = container.transform;

            fluidVisualGrid.GetVisualLayerForWorldLayer = (worldLayer) => fluidVisualLayer;

            Debug.Log($"[GridManager] Created fluid visual grid using layer: {fluidVisualLayer.LayerName}");
        }

        public void AddLayerToCollection(WorldLayer layer)
        {
            if (worldLayerCollection == null || layer == null) return;

            if (!worldLayerCollection.Layers.Contains(layer))
            {
                worldLayerCollection.Layers.Add(layer);
                CreateVisualGridForLayer(layer);
                InitializeLayerTexture(layer);
            }
        }

        private void InitializeLayerTextures()
        {
            if (worldLayerCollection == null) return;

            foreach (var layer in worldLayerCollection.Layers)
                if (layer != null)
                    InitializeLayerTexture(layer);
        }

        private void InitializeLayerTexture(WorldLayer layer)
        {
            if (layer.PreviewTexture == null || layer.PreviewTexture.width != gridWidth ||
                layer.PreviewTexture.height != gridHeight)
                layer.InitializePreviewTexture(gridWidth, gridHeight);
        }

        private void SubscribeToEvents()
        {
            ToolEvents.OnTileChanged += HandleTileChanged;
            ToolEvents.OnGridCleared += HandleGridCleared;

            if (enableFluids)
            {
                ToolEvents.OnFluidSimulationTick += HandleFluidSimulationTick;
                ToolEvents.OnFluidPlaced += HandleFluidChanged;
                ToolEvents.OnFluidRemoved += HandleFluidRemoved;
            }
        }

        private void UnsubscribeFromEvents()
        {
            ToolEvents.OnTileChanged -= HandleTileChanged;
            ToolEvents.OnGridCleared -= HandleGridCleared;

            if (enableFluids)
            {
                ToolEvents.OnFluidSimulationTick -= HandleFluidSimulationTick;
                ToolEvents.OnFluidPlaced -= HandleFluidChanged;
                ToolEvents.OnFluidRemoved -= HandleFluidRemoved;
            }
        }

        private void HandleGridCleared()
        {
            foreach (var visualGrid in visualGrids.Values)
                visualGrid?.Clear();
            fluidVisualGrid?.Clear();
        }

        private void HandleTileChanged(int x, int y)
        {
            foreach (var kvp in visualGrids)
            {
                var visualGrid = kvp.Value;
                visualGrid?.UpdateVisualTile(x - 1, y - 1);
                visualGrid?.UpdateVisualTile(x, y - 1);
                visualGrid?.UpdateVisualTile(x - 1, y);
                visualGrid?.UpdateVisualTile(x, y);
            }
        }

        private void HandleFluidSimulationTick()
        {
            if (fluidVisualGrid == null) return;
            fluidVisualGrid.UpdateAllVisuals();
        }

        private void HandleFluidChanged(WorldTile tile)
        {
            if (fluidVisualGrid == null)
            {
                Debug.LogWarning("[GridManager] fluidVisualGrid is null!");
                return;
            }

            var x = tile.GridPosition.x;
            var y = tile.GridPosition.y;

            Debug.Log($"[GridManager] HandleFluidChanged at ({x}, {y}) FillLevel: {tile.Fluid.FillLevel}");

            fluidVisualGrid.UpdateVisualFluidTile(x - 1, y - 1);
            fluidVisualGrid.UpdateVisualFluidTile(x, y - 1);
            fluidVisualGrid.UpdateVisualFluidTile(x - 1, y);
            fluidVisualGrid.UpdateVisualFluidTile(x, y);
        }

        private void HandleFluidRemoved(Vector3Int position)
        {
            if (fluidVisualGrid == null) return;

            var x = position.x;
            var y = position.y;
            fluidVisualGrid.UpdateVisualFluidTile(x - 1, y - 1);
            fluidVisualGrid.UpdateVisualFluidTile(x, y - 1);
            fluidVisualGrid.UpdateVisualFluidTile(x - 1, y);
            fluidVisualGrid.UpdateVisualFluidTile(x, y);
        }

        public void PlaceTile(int x, int y, WorldLayer layer, ToolType toolType)
        {
            var level = layer.LayerLevel;

            switch (toolType)
            {
                case ToolType.Paint:
                    WorldGrid.PlaceTile(x, y, layer);
                    break;

                case ToolType.Erase:
                    WorldGrid.RemoveTile(x, y, level);
                    break;
            }
        }

        public void RemoveAllTilesForLayer(WorldLayer layer)
        {
            if (layer == null || WorldGrid == null) return;

            var tilesToRemove = new List<Vector3Int>();

            foreach (var position in WorldGrid.GetAllPositions())
            {
                var tile = WorldGrid.GetTile(position.x, position.y, position.z);
                if (tile != null && tile.SourceLayer == layer) tilesToRemove.Add(position);
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

            fluidVisualGrid?.Clear();
            fluidVisualGrid = null;
            if (visualTilesContainer != null)
            {
                if (Application.isPlaying)
                    Destroy(visualTilesContainer.gameObject);
                else
                    DestroyImmediate(visualTilesContainer.gameObject);

                visualTilesContainer = null;
            }

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

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!drawGizmos || WorldGrid == null)
                return;

            if (drawWorldGrid) DrawWorldGridGizmos();
            if (drawVisualGrid) DrawVisualGridGizmos();
            if (drawFluidGrid) DrawFluidGridGizmos();
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

        private void DrawFluidGridGizmos()
        {
            if (fluidVisualGrid == null) return;

            Gizmos.color = fluidGridColor;

            var offsetX = tileSize * 0.5f;
            var offsetZ = tileSize * 0.5f;

            for (var y = 0; y <= gridHeight - 1; y++)
            {
                var start = new Vector3(offsetX, 0.01f, y * tileSize + offsetZ);
                var end = new Vector3((gridWidth - 1) * tileSize + offsetX, 0.01f, y * tileSize + offsetZ);
                Gizmos.DrawLine(start, end);
            }

            for (var x = 0; x <= gridWidth - 1; x++)
            {
                var start = new Vector3(x * tileSize + offsetX, 0.01f, offsetZ);
                var end = new Vector3(x * tileSize + offsetX, 0.01f, (gridHeight - 1) * tileSize + offsetZ);
                Gizmos.DrawLine(start, end);
            }

            for (var x = 0; x < gridWidth - 1; x++)
            for (var y = 0; y < gridHeight - 1; y++)
            {
                var tile = fluidVisualGrid.GetTile(x, y);
                if (tile != null && tile.ConfigurationIndex > 0)
                {
                    var pos = GridToWorldPosition(x, y) + new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f);
                    pos.y = 0.01f;

                    Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.6f);

                    var crossSize = tileSize * 0.15f;
                    Gizmos.DrawLine(pos - Vector3.right * crossSize, pos + Vector3.right * crossSize);
                    Gizmos.DrawLine(pos - Vector3.forward * crossSize, pos + Vector3.forward * crossSize);
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

        [ContextMenu("Refresh Fluid Visuals")]
        public void EditorRefreshFluidVisuals()
        {
            if (fluidVisualGrid != null)
            {
                fluidVisualGrid.UpdateAllVisuals();
                Debug.Log("[GridManager] Manually refreshed fluid visuals");
            }
            else
            {
                Debug.LogWarning("[GridManager] No fluid visual grid to refresh");
            }
        }
#endif

        public void RefreshAllVisualGrids()
        {
            if (visualGrids == null)
            {
                Debug.LogWarning("[GridManager] visualGrids is null!");
                return;
            }

            foreach (var kvp in visualGrids)
            {
                var layer = kvp.Key;
                var visualGrid = kvp.Value;
                if (visualGrid.TilesContainer == null)
                {
                    Debug.LogWarning($"[GridManager] TilesContainer is null for layer {layer.LayerName} - recreating...");
                    RecreateVisualGridContainer(layer, visualGrid);
                }

                var visualLayer = visualLayerCollection?.GetVisualLayerForWorldLayer(layer);
                if (visualLayer == null)
                    Debug.LogWarning($"[GridManager] No VisualLayer found for WorldLayer '{layer.LayerName}'");

                visualGrid.UpdateAllVisuals();
            }

            if (fluidVisualGrid != null)
            {
                if (fluidVisualGrid.TilesContainer == null)
                {
                    Debug.LogWarning("[GridManager] Fluid TilesContainer is null - recreating...");
                    RecreateFluidVisualGridContainer();
                }

                fluidVisualGrid.UpdateAllVisuals();
            }
        }

        private void RecreateVisualGridContainer(WorldLayer layer, VisualGrid visualGrid)
        {
            if (visualTilesContainer == null)
            {
                var container = new GameObject("Visual Tiles");
                container.transform.SetParent(transform);
                container.hideFlags = HideFlags.DontSave;
                visualTilesContainer = container.transform;
            }

            var layerContainer = new GameObject($"Layer_{layer.LayerName}");
            layerContainer.transform.SetParent(visualTilesContainer);
            layerContainer.hideFlags = HideFlags.DontSave;

            var layerIndex = worldLayerCollection.Layers.IndexOf(layer);
            layerContainer.transform.localPosition = new Vector3(0, layerIndex * -0.05f, 0);

            visualGrid.TilesContainer = layerContainer.transform;
        }

        private void RecreateFluidVisualGridContainer()
        {
            if (visualTilesContainer == null)
            {
                var container = new GameObject("Visual Tiles");
                container.transform.SetParent(transform);
                container.hideFlags = HideFlags.DontSave;
                visualTilesContainer = container.transform;
            }

            var fluidContainer = new GameObject("Fluid_Visuals");
            fluidContainer.transform.SetParent(visualTilesContainer);
            fluidContainer.hideFlags = HideFlags.DontSave;
            fluidContainer.transform.localPosition = new Vector3(0, fluidVisualHeightOffset, 0);

            fluidVisualGrid.TilesContainer = fluidContainer.transform;
        }
    }
}