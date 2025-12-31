using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Data.Layers;
using UnityEngine;

namespace CozyWorldGeneration.Core.DualGrid
{
    public class WorldTile
    {
        public WorldTile(Vector2Int gridPosition, WorldLayer sourceLayer = null)
        {
            GridPosition = gridPosition;
            State = TileState.Normal;
            SourceLayer = sourceLayer;
        }

        public WorldTile(int x, int y, WorldLayer sourceLayer = null) : this(new Vector2Int(x, y),
            sourceLayer)
        {
        }

        public Vector2Int GridPosition { get; private set; }
        public TileState State { get; set; }
        public WorldLayer SourceLayer { get; set; }

        // TODO: IsWalkable/IsModifiable will be handled differently. 
        // Walkable through the collider generation so we don't actually need to know if it's walkable or not
        // Modifiable we will probably have a property in the future that we just check.
    }
}