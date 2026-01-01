using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Events;
using UnityEngine;
using System.Collections.Generic;

namespace CozyWorldGeneration.Core.DualGrid
{
    public class WorldGrid
    {
        // Key is (x, y, level)
        private Dictionary<Vector3Int, WorldTile> tiles = new();

        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool SuppressEvents { get; set; } = false;

        public WorldGrid(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void PlaceTile(int x, int y, WorldLayer layer)
        {
            if (!IsValidPosition(x, y) || layer == null) return;

            var key = new Vector3Int(x, y, layer.LayerLevel);
            tiles[key] = new WorldTile(x, y, layer);
            ToolEvents.RaiseTileChanged(x, y);
            if (!SuppressEvents)
                ToolEvents.RaiseTileChanged(x, y);
        }

        public void RemoveTile(int x, int y, int level)
        {
            var key = new Vector3Int(x, y, level);
            if (tiles.Remove(key) && !SuppressEvents)
                ToolEvents.RaiseTileChanged(x, y);
        }

        public WorldTile GetTile(int x, int y, int level)
        {
            var key = new Vector3Int(x, y, level);
            tiles.TryGetValue(key, out var tile);
            return tile;
        }

        public bool HasTileAt(int x, int y, int level)
        {
            return GetTile(x, y, level) != null;
        }

        /// <summary>
        /// Checks if a specific layer has a tile at this position.
        /// </summary>
        public bool HasTileForLayer(int x, int y, WorldLayer layer)
        {
            var tile = GetTile(x, y, layer.LayerLevel);
            return tile != null && tile.SourceLayer == layer;
        }

        public IEnumerable<Vector3Int> GetAllPositions()
        {
            return tiles.Keys;
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return IsValidPosition(pos.x, pos.y);
        }

        public void Clear()
        {
            tiles.Clear();
            if (!SuppressEvents)
                ToolEvents.RaiseGridCleared();
        }
    }
}