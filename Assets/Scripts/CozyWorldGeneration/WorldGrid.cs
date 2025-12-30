using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration
{
    public class WorldGrid : Grid<WorldTile>
    {
        private VisualGrid visualGrid;

        public WorldGrid(int width, int height) : base(width, height)
        {
        }

        /// <summary>
        /// Links this WorldGrid to a VisualGrid for automatic visual updates.
        /// </summary>
        public void LinkVisualGrid(VisualGrid visualGrid)
        {
            this.visualGrid = visualGrid;
        }

        /// <summary>
        /// Sets a tile and notifies the VisualGrid to update affected visual tiles.
        /// </summary>
        public override void SetTile(int x, int y, WorldTile tile)
        {
            base.SetTile(x, y, tile);

            // Notify affected visual tiles
            if (visualGrid != null) NotifyVisualGridUpdate(x, y);
        }

        /// <summary>
        /// Creates and places a new tile at the specified position.
        /// </summary>
        public void PlaceTile(int x, int y, TileType type)
        {
            var tile = new WorldTile(x, y, type);
            SetTile(x, y, tile);
        }

        /// <summary>
        /// Modifies the state of a tile (e.g., digging, tilling).
        /// </summary>
        public void ModifyTileState(int x, int y, TileState newState)
        {
            var tile = GetTile(x, y);
            if (tile != null && tile.IsModifiable())
            {
                tile.State = newState;

                // Visual update (state changes might affect visuals)
                if (visualGrid != null) NotifyVisualGridUpdate(x, y);
            }
        }

        /// <summary>
        /// Gets the TileType at a position (or None if no tile exists).
        /// Convenient helper for visual grid calculations.
        /// </summary>
        public TileType GetTileTypeAt(int x, int y)
        {
            var tile = GetTile(x, y);
            return tile != null ? tile.Type : TileType.None;
        }

        /// <summary>
        /// Notifies the VisualGrid that tiles around this position need updating.
        /// A WorldTile at (x, y) affects the 4 VisualTiles that overlap it.
        /// Using the same offset pattern as the reference implementation.
        /// </summary>
        private void NotifyVisualGridUpdate(int x, int y)
        {
            // The 4 visual tiles affected by this world tile
            // These are the positions where this world tile is one of the 4 neighbors
            var affectedVisualTiles = new Vector2Int[]
            {
                new(x, y), // This world tile is bottom-left neighbor
                new(x - 1, y), // This world tile is bottom-right neighbor
                new(x, y - 1), // This world tile is top-left neighbor
                new(x - 1, y - 1) // This world tile is top-right neighbor
            };

            foreach (var pos in affectedVisualTiles) visualGrid?.UpdateVisualTile(pos.x, pos.y);
        }
    }
}