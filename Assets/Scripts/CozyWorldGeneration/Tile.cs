using CozyWorldGeneration.Layers;
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

    /// <summary>
    /// Simplified visual tile - just stores position, configuration, and visual reference.
    /// Much lighter than the previous implementation.
    /// </summary>
    public class VisualTile
    {
        public Vector2Int GridPosition { get; private set; }
        public int ConfigurationIndex { get; set; }
        public GameObject VisualInstance { get; set; }

        public VisualTile(int x, int y)
        {
            GridPosition = new Vector2Int(x, y);
            ConfigurationIndex = 0;
        }

        /// <summary>
        /// Updates the visual representation based on configuration index.
        /// TODO: Implement mesh/prefab system here.
        /// </summary>
        public void UpdateVisual()
        {
            // Placeholder for visual update logic
            // You'll load the appropriate mesh/prefab for ConfigurationIndex here

#if UNITY_EDITOR
            if (ConfigurationIndex > 0) Debug.Log($"VisualTile at {GridPosition} -> Config {ConfigurationIndex}");
#endif
        }

        /// <summary>
        /// Destroys the visual GameObject if it exists.
        /// </summary>
        public void DestroyVisual()
        {
            if (VisualInstance != null)
            {
                Object.Destroy(VisualInstance);
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