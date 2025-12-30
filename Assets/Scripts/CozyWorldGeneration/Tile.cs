using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Data.Tilesets;
using UnityEngine;

namespace CozyWorldGeneration
{
    // This will be the tiles of our main Grid. It will hold data state.
    public class WorldTile
    {
        public WorldTile(Vector2Int gridPosition, TileType type, WorldLayer sourceLayer = null)
        {
            GridPosition = gridPosition;
            Type = type;
            State = TileState.Normal;
            SourceLayer = sourceLayer;
        }

        public WorldTile(int x, int y, TileType type, WorldLayer sourceLayer = null) : this(new Vector2Int(x, y), type,
            sourceLayer)
        {
        }

        public Vector2Int GridPosition { get; private set; }
        public TileType Type { get; set; }
        public TileState State { get; set; }
        public WorldLayer SourceLayer { get; set; }

        public bool IsWalkable()
        {
            return Type != TileType.None; // Will add more in the future;
        }

        public bool IsModifiable()
        {
            return Type != TileType.None; // Will add more in the future;
        }
    }

    public class VisualTile
    {
        public Vector2Int GridPosition { get; private set; }
        public int ConfigurationIndex { get; set; }
        public GameObject VisualInstance { get; set; }

        private VisualLayer visualLayer;
        private Tileset selectedTileset;

        public VisualTile(int x, int y)
        {
            GridPosition = new Vector2Int(x, y);
            ConfigurationIndex = 0;
        }

        /// <summary>
        /// Sets the visual layer this tile should use for rendering.
        /// </summary>
        public void SetVisualLayer(VisualLayer layer)
        {
            visualLayer = layer;
            // Select a random tileset when layer is assigned
            if (layer != null) selectedTileset = layer.GetRandomTileset();
        }

        /// <summary>
        /// Updates the visual representation based on configuration index and tileset.
        /// </summary>
        public void UpdateVisual(Transform parent, float tileSize)
        {
            // Destroy old visual if it exists
            if (VisualInstance != null)
            {
                Object.DestroyImmediate(VisualInstance);
                VisualInstance = null;
            }

            // If no tileset or config is empty, don't render
            if (selectedTileset == null || ConfigurationIndex == 0)
                return;

            // Get the configuration from the tileset
            var config = selectedTileset.GetConfiguration(ConfigurationIndex);

            // If no mesh, don't render
            if (config.mesh == null)
                return;

            // Get or create layer-specific container
            var layerContainer = GetOrCreateLayerContainer(parent);

            // Create visual instance
            VisualInstance = new GameObject($"VisualTile_{GridPosition.x}_{GridPosition.y}");
            VisualInstance.transform.SetParent(layerContainer);

            // Position and rotate
            var worldPos = new Vector3(
                (GridPosition.x + 0.5f) * tileSize,
                visualLayer != null ? visualLayer.DefaultLayerHeight : 0,
                (GridPosition.y + 0.5f) * tileSize
            );
            VisualInstance.transform.position = worldPos;
            VisualInstance.transform.rotation = config.GetRotation();

            // Add mesh components
            var meshFilter = VisualInstance.AddComponent<MeshFilter>();
            meshFilter.mesh = config.mesh;

            var meshRenderer = VisualInstance.AddComponent<MeshRenderer>();
            meshRenderer.material = config.material;
        }

        /// <summary>
        /// Gets or creates a container for this visual layer.
        /// </summary>
        private Transform GetOrCreateLayerContainer(Transform parent)
        {
            if (visualLayer == null || parent == null)
                return parent;

            var containerName = $"Layer_{visualLayer.LayerName}";
            var existing = parent.Find(containerName);

            if (existing != null)
                return existing;

            var container = new GameObject(containerName);
            container.transform.SetParent(parent);
            container.transform.localPosition = Vector3.zero;
            return container.transform;
        }

        /// <summary>
        /// Destroys the visual GameObject if it exists.
        /// </summary>
        public void DestroyVisual()
        {
            if (VisualInstance != null)
            {
                Object.DestroyImmediate(VisualInstance);
                VisualInstance = null;
            }
        }
    }

// TODO: Check if it's possible to make enum types as Scriptable Objects so we can just add them
// from a file faster instead of having to open the code each time
    public enum TileType
    {
        None,
        Grass,
        Dirt,
        Stone,
        Water,
        Sand
    }

// This is mostly for grass but we'll see
    public enum TileState
    {
        Normal,
        Dug,
        Tilled,
        Waterlogged
    }
}