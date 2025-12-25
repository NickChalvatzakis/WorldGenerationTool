using UnityEngine;

namespace CozyWorldGeneration
{
    // This will be the tiles of our main Grid. It will hold data state.
    public class WorldTile
    {
        public WorldTile(Vector2Int gridPosition, TileType type)
        {
            GridPosition = gridPosition;
            Type = type;
            State = TileState.Normal;
        }

        public WorldTile(int x, int y, TileType type) : this(new Vector2Int(x, y), type)
        {
        }

        public Vector2Int GridPosition { get; private set; }
        public TileType Type { get; set; }
        public TileState State { get; set; }

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
        private WorldGrid worldGrid;

        public VisualTile(Vector2Int gridPosition, WorldGrid worldGrid)
        {
            GridPosition = gridPosition;
            this.worldGrid = worldGrid;
            ConfigurationIndex = 0;
        }

        public VisualTile(int x, int y, WorldGrid worldGrid) : this(
            new Vector2Int(x, y), worldGrid)
        {
        }

        public Vector2Int GridPosition { get; set; }
        public int ConfigurationIndex { get; set; }
        public GameObject VisualInstance { get; set; }

        // Update the visual configuration based on the 4 overlapping WorldGrid tiles.
        // the 4 tiles are x,y x+1,y x, y+1 x+1,y+1
        // so we have 16 different configurations 
        public void UpdateVisual()
        {
            // var x = GridPosition.x;
            // var y = GridPosition.y;
            //
            // var config = 0;
            //
            // if (IsTileFilled(x, y)) config |= 1;
            // if (IsTileFilled(x + 1, y)) config |= 2;
            // if (IsTileFilled(x, y + 1)) config |= 4;
            // if (IsTileFilled(x + 1, y + 1)) config |= 8;

            // ConfigurationIndex = config;

            UpdateMesh();
        }

        // private bool IsTileFilled(int x, int y)
        // {
        //     var tile = worldGrid.GetTile(x, y);
        //     return tile != null && tile.Type != TileType.None;
        // }

        private void UpdateMesh()
        {
            // TODO: we will probably make the meshes procedurally, but we'll see
            Debug.Log($"VisualTile at {GridPosition} update to config {ConfigurationIndex}");
        }

        public void DestroyVisual()
        {
            if (VisualInstance != null)
            {
                Object.Destroy(VisualInstance);
                VisualInstance = null;
            }
        }
    }

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