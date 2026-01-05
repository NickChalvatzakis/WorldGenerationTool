using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration.Core
{
    /// <summary>
    /// Generic base class for 3D grid types (x, y, level).
    /// Uses Dictionary for efficient sparse storage.
    /// </summary>
    public abstract class Grid3D<T> where T : class
    {
        protected Dictionary<Vector3Int, T> tiles;

        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public int MaxLevels { get; protected set; }

        protected Grid3D(int width, int height, int maxLevels)
        {
            Width = width;
            Height = height;
            MaxLevels = maxLevels;
            tiles = new Dictionary<Vector3Int, T>();
        }

        public T GetTile(int x, int y, int level)
        {
            var key = new Vector3Int(x, y, level);
            tiles.TryGetValue(key, out var tile);
            return tile;
        }

        public T GetTile(Vector3Int position)
        {
            tiles.TryGetValue(position, out var tile);
            return tile;
        }

        public virtual void SetTile(int x, int y, int level, T tile)
        {
            var key = new Vector3Int(x, y, level);

            if (tile == null)
                tiles.Remove(key);
            else
                tiles[key] = tile;
        }

        public void SetTile(Vector3Int position, T tile)
        {
            SetTile(position.x, position.y, position.z, tile);
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return IsValidPosition(position.x, position.y);
        }

        public bool IsValidPosition(int x, int y, int level)
        {
            return IsValidPosition(x, y) && level >= 0 && level < MaxLevels;
        }

        /// <summary>
        /// Vector3Int (x,y,level)
        /// </summary>
        public bool IsValidPosition(Vector3Int position)
        {
            return IsValidPosition(position.x, position.y) && position.z >= 0 && position.z < MaxLevels;
        }

        public bool HasTile(int x, int y, int level)
        {
            return tiles.ContainsKey(new Vector3Int(x, y, level));
        }

        public bool HasTile(Vector3Int position)
        {
            return tiles.ContainsKey(position);
        }

        /// <summary>
        /// Gets all tiles at a specific (x, y) position across all levels.
        /// </summary>
        public List<T> GetTilesAtPosition(int x, int y)
        {
            var result = new List<T>();
            foreach (var kvp in tiles)
                if (kvp.Key.x == x && kvp.Key.y == y)
                    result.Add(kvp.Value);
            return result;
        }

        /// <summary>
        /// Gets all tiles at a specific level.
        /// </summary>
        public IEnumerable<T> GetTilesAtLevel(int level)
        {
            foreach (var kvp in tiles)
                if (kvp.Key.z == level)
                    yield return kvp.Value;
        }

        public IEnumerable<T> GetAllTiles()
        {
            return tiles.Values;
        }

        public IEnumerable<Vector3Int> GetAllPositions()
        {
            return tiles.Keys;
        }

        public int GetTileCount()
        {
            return tiles.Count;
        }

        public virtual void Clear()
        {
            tiles.Clear();
        }
    }
}