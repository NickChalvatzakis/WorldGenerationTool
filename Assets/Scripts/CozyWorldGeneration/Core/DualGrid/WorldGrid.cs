using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Events;
using UnityEngine;
using System.Collections.Generic;

namespace CozyWorldGeneration.Core.DualGrid
{
    public class WorldGrid
    {
        private Dictionary<Vector3Int, WorldTile> tiles = new(); // x, y, level

        public int Width { get; private set; }
        public int Height { get; private set; }

        public WorldGrid(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void PlaceTile(int x, int y, int level, WorldLayer layer)
        {
            if (!IsValidPosition(x, y)) return;

            var key = new Vector3Int(x, y, level);
            tiles[key] = new WorldTile(x, y, layer);
            ToolEvents.RaiseTileChanged(x, y);
        }

        public void RemoveTile(int x, int y, int level)
        {
            var key = new Vector3Int(x, y, level);
            if (tiles.Remove(key))
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
        /// Gets all tiles at a position across all levels.
        /// </summary>
        public IEnumerable<WorldTile> GetAllTilesAt(int x, int y)
        {
            foreach (var kvp in tiles)
                if (kvp.Key.x == x && kvp.Key.y == y)
                    yield return kvp.Value;
        }

        /// <summary>
        /// Gets the top-most tile at a position.
        /// </summary>
        public WorldTile GetTopTile(int x, int y)
        {
            WorldTile topTile = null;
            var topLevel = int.MinValue;

            foreach (var kvp in tiles)
                if (kvp.Key.x == x && kvp.Key.y == y && kvp.Key.z > topLevel)
                {
                    topLevel = kvp.Key.z;
                    topTile = kvp.Value;
                }

            return topTile;
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return IsValidPosition(pos.x, pos.y);
        }

        public IEnumerable<Vector3Int> GetAllPositions()
        {
            return tiles.Keys;
        }

        public void Clear()
        {
            tiles.Clear();
            ToolEvents.RaiseGridCleared();
        }
    }
}