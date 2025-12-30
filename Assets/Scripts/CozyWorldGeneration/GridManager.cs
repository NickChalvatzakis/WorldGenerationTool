using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Events;
using UnityEngine;

namespace CozyWorldGeneration
{
    /// <summary>
    /// Central manager for the dual grid system.
    /// Handles creation, initialization, and coordination between WorldGrid and VisualGrid.
    /// Use this as the main entry point for editor tools.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")] [SerializeField]
        private int gridWidth = 20;

        [SerializeField] private int gridHeight = 20;
        [SerializeField] private float tileSize = 1f;

        [Header("Layer Collections")] [SerializeField]
        private WorldLayerCollection worldLayerCollection;

        [SerializeField] private VisualLayerCollection visualLayerCollection;

        [Header("Debug")] [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawWorldGrid = true;
        [SerializeField] private bool drawVisualGrid = true;
        [SerializeField] private Color worldGridColor = new(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color visualGridColor = new(0.2f, 0.2f, 0.2f);

        public WorldGrid WorldGrid { get; private set; }
        public VisualGrid VisualGrid { get; private set; }

        public int Width => gridWidth;
        public int Height => gridHeight;
        public float TileSize => tileSize;

        public WorldLayerCollection WorldLayerCollection => worldLayerCollection;
        public VisualLayerCollection VisualLayerCollection => visualLayerCollection;

        private Transform visualTilesContainer;

        public Transform VisualTilesContainer => visualTilesContainer;

        private void Awake()
        {
            InitializeGrids();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
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
            if (WorldGrid == null || VisualGrid == null) InitializeGrids();
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

            // Create main container for visual tiles if it doesn't exist
            if (visualTilesContainer == null)
            {
                var container = new GameObject("Visual Tiles");
                container.transform.SetParent(transform);
                visualTilesContainer = container.transform;
            }

            WorldGrid = new WorldGrid(gridWidth, gridHeight);
            VisualGrid = new VisualGrid(gridWidth - 1, gridHeight - 1, WorldGrid, tileSize);
            WorldGrid.LinkVisualGrid(VisualGrid);

            // Set the main tiles container
            VisualGrid.TilesContainer = visualTilesContainer;

            // Set the delegate to find VisualLayers
            VisualGrid.GetVisualLayerForWorldLayer = (worldLayer) =>
            {
                return visualLayerCollection?.GetVisualLayerForWorldLayer(worldLayer);
            };

            InitializeLayerTextures();
            ToolEvents.RaiseGridInitialized(gridWidth, gridHeight);

            Debug.Log(
                $"GridManager initialized: WorldGrid ({gridWidth}x{gridHeight}), VisualGrid ({gridWidth - 1}x{gridHeight - 1})");
        }

        private void InitializeLayerTextures()
        {
            foreach (var layer in worldLayerCollection.Layers) layer?.InitializePreviewTexture(gridWidth, gridHeight);
        }

        private void SubscribeToEvents()
        {
            ToolEvents.OnLayerAdded += HandleLayerAdded;
            ToolEvents.OnLayerRemoved += HandleLayerRemoved;
            ToolEvents.OnLayerCleared += HandleLayerCleared;
            ToolEvents.OnPixelPainted += HandlePixelPainted;
        }

        private void UnsubscribeFromEvents()
        {
            ToolEvents.OnLayerAdded -= HandleLayerAdded;
            ToolEvents.OnLayerRemoved -= HandleLayerRemoved;
            ToolEvents.OnLayerCleared -= HandleLayerCleared;
            ToolEvents.OnPixelPainted -= HandlePixelPainted;
        }

        private void HandleLayerAdded(WorldLayer layer)
        {
            if (layer != null) layer.InitializePreviewTexture(gridWidth, gridHeight);
        }

        private void HandleLayerRemoved(WorldLayer layer)
        {
            if (layer != null) ClearTilesFromLayer(layer);
        }

        private void HandleLayerCleared(WorldLayer layer)
        {
            ClearTilesFromLayer(layer);
        }

        private void HandlePixelPainted(WorldLayer layer, int x, int y, bool painted)
        {
            // Hook for future features (undo/redo, auto-save, etc.)
        }

        private void ClearTilesFromLayer(WorldLayer layer)
        {
            if (WorldGrid == null) return;

            var tilesToRemove = new System.Collections.Generic.List<Vector2Int>();

            foreach (var position in WorldGrid.GetAllPositions())
            {
                var tile = WorldGrid.GetTile(position);
                if (tile != null && tile.SourceLayer == layer) tilesToRemove.Add(position);
            }

            foreach (var position in tilesToRemove) WorldGrid.SetTile(position.x, position.y, null);

#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        public void PlaceTile(int x, int y, TileType type)
        {
            WorldGrid?.PlaceTile(x, y, type);
        }

        public void RemoveTile(int x, int y)
        {
            WorldGrid?.SetTile(x, y, null);
        }

        public void ModifyTile(int x, int y, TileState newState)
        {
            WorldGrid?.ModifyTileState(x, y, newState);
        }

        public WorldTile GetWorldTile(int x, int y)
        {
            return WorldGrid?.GetTile(x, y);
        }

        public VisualTile GetVisualTile(int x, int y)
        {
            return VisualGrid?.GetTile(x, y);
        }

        public int GetVisualConfiguration(int x, int y)
        {
            var tile = VisualGrid?.GetTile(x, y);
            return tile?.ConfigurationIndex ?? 0;
        }

        public void ClearGrids()
        {
            VisualGrid?.Clear();
            WorldGrid?.Clear();
            Debug.Log("Grids cleared");
        }

        public Vector3 GridToWorldPosition(int x, int y, bool isVisualGrid = false)
        {
            if (isVisualGrid)
                return VisualGrid.GetWorldPosition(x, y, tileSize);
            else
                return new Vector3((x + 0.5f) * tileSize, 0f, (y + 0.5f) * tileSize);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            var x = Mathf.FloorToInt(worldPos.x / tileSize);
            var y = Mathf.FloorToInt(worldPos.z / tileSize);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// Rebuilds all visual tiles from the current WorldGrid state.
        /// </summary>
        [ContextMenu("Rebuild All Visuals")]
        public void RebuildAllVisuals()
        {
            if (VisualGrid == null)
            {
                Debug.LogWarning("VisualGrid is null, cannot rebuild visuals");
                return;
            }

            Debug.Log("Rebuilding all visual tiles...");
            VisualGrid.UpdateAllVisuals();
            Debug.Log("Visual rebuild complete");
        }

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (!drawGizmos || WorldGrid == null || VisualGrid == null)
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

            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
            {
                var tile = WorldGrid.GetTile(x, y);
                if (tile != null && tile.Type != TileType.None)
                {
                    var tileColor = tile.SourceLayer != null ? tile.SourceLayer.LayerColor : Color.white;
                    tileColor.a = 0.3f;
                    Gizmos.color = tileColor;

                    var corners = new Vector3[4]
                    {
                        new(x * tileSize, 0.01f, y * tileSize),
                        new((x + 1) * tileSize, 0.01f, y * tileSize),
                        new((x + 1) * tileSize, 0.01f, (y + 1) * tileSize),
                        new(x * tileSize, 0.01f, (y + 1) * tileSize)
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
            {
                var tile = VisualGrid.GetTile(x, y);
                if (tile != null && tile.ConfigurationIndex > 0)
                {
                    var pos = GridToWorldPosition(x, y, true) + new Vector3(tileSize * 0.5f, 0, tileSize * 0.5f);
                    pos.y = 0.02f;

                    Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);

                    var crossSize = tileSize * 0.1f;
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
            ClearGrids();
            InitializeGrids();
        }

        [ContextMenu("Fill Test Pattern")]
        public void FillTestPattern()
        {
            if (WorldGrid == null) InitializeGrids();

            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
                if ((x + y) % 2 == 0)
                    PlaceTile(x, y, TileType.Grass);

            Debug.Log("Test pattern created");
        }
#endif
    }
}