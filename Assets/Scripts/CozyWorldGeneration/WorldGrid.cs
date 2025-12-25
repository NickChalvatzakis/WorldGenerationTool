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

        public void LinkVisualGrid(VisualGrid visualGrid)
        {
            this.visualGrid = visualGrid;
        }

        public override void SetTile(int x, int y, WorldTile tile)
        {
            base.SetTile(x, y, tile);
            if (visualGrid != null) NotifyVisualGridUpdate(x, y);
        }

        public void PlaceTile(int x, int y, TileType type)
        {
            var tile = new WorldTile(x, y, type);
            SetTile(x, y, tile);
        }

        public void RemoveTile(int x, int y)
        {
            SetTile(x, y, null);
        }

        public void ModifyTileState(int x, int y, TileState state)
        {
            var tile = GetTile(x, y);
            if (tile != null && tile.IsModifiable()) tile.State = state;

            if (visualGrid != null) NotifyVisualGridUpdate(x, y);
        }

        private void NotifyVisualGridUpdate(int x, int y)
        {
            var affectedVisualPositions = new Vector2Int[]
            {
                new (x, y),       
                new (x - 1, y),   
                new (x, y - 1),   
                new (x - 1, y - 1) 
            };

            foreach (var pos in affectedVisualPositions) visualGrid?.UpdateVisualTile(pos.x, pos.y);
        }

        public TileType GetTileTypeAt(int x, int y)
        {
            return GetTile(x, y).Type;
        }
    }
}