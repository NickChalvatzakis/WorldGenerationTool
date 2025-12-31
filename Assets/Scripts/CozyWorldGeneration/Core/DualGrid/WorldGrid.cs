using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Data.Layers;
using UnityEngine;

namespace CozyWorldGeneration.Core.DualGrid
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

        public void PlaceTile(int x, int y, WorldLayer selectedLayer)
        {
            var tile = new WorldTile(x, y, selectedLayer);
            SetTile(x, y, tile);
        }

        /// <summary>
        /// Checks if a tile exists at a position (for visual grid calculations).
        /// </summary>
        public bool HasTileAt(int x, int y)
        {
            return GetTile(x, y) != null;
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