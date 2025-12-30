using CozyWorldGeneration.Layers;
using UnityEngine;

namespace CozyWorldGeneration
{
    public class GridManager : MonoBehaviour
    {
        [Header("Grid Settings")] [SerializeField]
        private int gridWidth = 10;

        [SerializeField] private int gridHeight = 10;
        [SerializeField] private float tileSize = 1f;

        [Header("Layer Collections")] [SerializeField]
        private LayerCollection worldLayerCollection;

        [SerializeField] private LayerCollection visualLayerCollection;


        [Header("Debug")] [SerializeField] private bool drawGizmos = true;
        [SerializeField] private bool drawWorldGrid = true;
        [SerializeField] private bool drawVisualGrid = true;

        public WorldGrid WorldGrid { get; private set; }
        public VisualGrid VisualGrid { get; private set; }

        public int Width => gridWidth;
        public int Height => gridHeight;
        public float TileSize => tileSize;

        public LayerCollection WorldLayerCollection => worldLayerCollection;
        public LayerCollection VisualLayerCollection => visualLayerCollection;


        private void Awake()
        {
            InitializeGrids();
        }


        public void InitializeGrids()
        {
            if (worldLayerCollection == null) worldLayerCollection = new LayerCollection("World Layers");

            if (visualLayerCollection == null) visualLayerCollection = new LayerCollection("Visual Layers");

            WorldGrid = new WorldGrid(gridWidth, gridHeight);
            VisualGrid = new VisualGrid(gridWidth - 1, gridHeight - 1, WorldGrid);
            WorldGrid.LinkVisualGrid(VisualGrid);

            Debug.Log("Grid Manager Initialized");
        }

        private void InitializeLayerTextures()
        {
            foreach (var layer in worldLayerCollection.Layers) layer?.InitializePreviewTexture(gridWidth, gridHeight);
            foreach (var layer in visualLayerCollection.Layers)
                layer?.InitializePreviewTexture(gridWidth - 1, gridHeight - 1);
        }

        public void PlaceTile(int x, int y, TileType type)
        {
            WorldGrid?.PlaceTile(x, y, type);
        }

        public void ModifyTile(int x, int y, TileState state)
        {
            WorldGrid?.ModifyTileState(x, y, state);
        }

        public WorldTile GetWorldTile(int x, int y)
        {
            return WorldGrid?.GetTile(x, y);
        }

        public VisualTile GetVisualTile(int x, int y)
        {
            return VisualGrid?.GetTile(x, y);
        }

        public void ClearGrids()
        {
            VisualGrid?.Clear();
            WorldGrid?.Clear();
            Debug.Log("Grid Cleared");
        }

        public Vector3 GridToWorldPosition(int x, int y, bool isVisualGrid = false)
        {
            return isVisualGrid
                ? VisualGrid.GetWorldPosition(x, y, tileSize)
                : new Vector3(x * tileSize, 0f, y * tileSize);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            var x = Mathf.FloorToInt(worldPosition.x / tileSize);
            var y = Mathf.FloorToInt(worldPosition.z / tileSize);
            return new Vector2Int(x, y);
        }

        private void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            if (drawWorldGrid) DrawWorldGridGizmos();
            if (drawVisualGrid) DrawVisualGridGizmos();
        }

        private void DrawWorldGridGizmos()
        {
            Gizmos.color = Color.white;

            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
            {
                var position = GridToWorldPosition(x, y);
                var size = new Vector3(1f, .01f, 1f);
                Gizmos.DrawWireCube(position, size * tileSize);
            }
        }

        private void DrawVisualGridGizmos()
        {
            Gizmos.color = Color.blanchedAlmond;
            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
            {
                var position = GridToWorldPosition(x, y, true);
                var size = new Vector3(1f, .01f, 1f);

                Gizmos.DrawWireCube(position, size * tileSize);
            }
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


        [ContextMenu("ReInitialize Grid")]
        public void EditorInitializeGrid()
        {
            ClearGrids();
            InitializeGrids();
        }

        [ContextMenu("Fill Test")]
        public void FillTest()
        {
            if (WorldGrid == null) InitializeGrids();

            for (var x = 0; x < gridWidth; x++)
            for (var y = 0; y < gridHeight; y++)
                if ((x + 2) % 2 == 0)
                    PlaceTile(x, y, TileType.Grass);
        }
#endif
    }
}