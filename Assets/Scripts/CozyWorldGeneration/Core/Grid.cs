using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration.Core
{
    /// <summary>
    /// Generic base class for all grid types in the system.
    /// Handles spatial logic, bounds checking, and neighbor queries.
    /// Uses a Dictionary for efficient sparse storage.
    /// </summary>
    /// <typeparam name="T">The tile type. Must be a reference type (class).</typeparam>
    public abstract class Grid<T> where T : class
    {
        // Dictionary stores only tiles that exist, perfect for sparse grids
        protected Dictionary<Vector2Int, T> tiles;

        public int Width { get; protected set; }
        public int Height { get; protected set; }

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Dictionary<Vector2Int, T>();
        }

        /// <summary>
        /// Gets the tile at the specified grid position.
        /// Returns null if no tile exists at that position.
        /// </summary>
        public T GetTile(int x, int y)
        {
            var key = new Vector2Int(x, y);
            tiles.TryGetValue(key, out var tile);
            return tile;
        }

        /// <summary>
        /// Gets the tile at the specified grid position.
        /// </summary>
        public T GetTile(Vector2Int position)
        {
            tiles.TryGetValue(position, out var tile);
            return tile;
        }

        /// <summary>
        /// Sets the tile at the specified grid position.
        /// If tile is null, removes the entry from the dictionary.
        /// </summary>
        public virtual void SetTile(int x, int y, T tile)
        {
            var key = new Vector2Int(x, y);

            if (tile == null)
                // Remove from dictionary if setting to null
                tiles.Remove(key);
            else
                // Add or update the tile
                tiles[key] = tile;
        }

        /// <summary>
        /// Sets the tile at the specified grid position.
        /// </summary>
        public void SetTile(Vector2Int position, T tile)
        {
            SetTile(position.x, position.y, tile);
        }

        /// <summary>
        /// Checks if the position is within grid bounds.
        /// Note: With Dictionary, you can technically store tiles outside bounds,
        /// but this method enforces your defined grid size.
        /// </summary>
        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Checks if the position is within grid bounds.
        /// </summary>
        public bool IsValidPosition(Vector2Int position)
        {
            return IsValidPosition(position.x, position.y);
        }

        /// <summary>
        /// Checks if a tile exists at the specified position.
        /// </summary>
        public bool HasTile(int x, int y)
        {
            return tiles.ContainsKey(new Vector2Int(x, y));
        }

        /// <summary>
        /// Checks if a tile exists at the specified position.
        /// </summary>
        public bool HasTile(Vector2Int position)
        {
            return tiles.ContainsKey(position);
        }

        /// <summary>
        /// Gets the four cardinal neighbors (up, down, left, right) of a position.
        /// Only returns neighbors that exist and are within grid bounds.
        /// </summary>
        public List<T> GetCardinalNeighbors(int x, int y)
        {
            var neighbors = new List<T>(4);

            // Up, Right, Down, Left
            Vector2Int[] directions =
            {
                new(0, 1), // Up
                new(1, 0), // Right
                new(0, -1), // Down
                new(-1, 0) // Left
            };

            foreach (var dir in directions)
            {
                var nx = x + dir.x;
                var ny = y + dir.y;

                var neighbor = GetTile(nx, ny);
                if (neighbor != null) neighbors.Add(neighbor);
            }

            return neighbors;
        }

        /// <summary>
        /// Gets the four cardinal neighbors of a position.
        /// </summary>
        public List<T> GetCardinalNeighbors(Vector2Int position)
        {
            return GetCardinalNeighbors(position.x, position.y);
        }

        /// <summary>
        /// Iterates through all tiles that actually exist in the grid.
        /// Much more efficient than iterating empty spaces with Dictionary.
        /// </summary>
        public IEnumerable<T> GetAllTiles()
        {
            return tiles.Values;
        }

        /// <summary>
        /// Gets all tile positions that have tiles.
        /// </summary>
        public IEnumerable<Vector2Int> GetAllPositions()
        {
            return tiles.Keys;
        }

        /// <summary>
        /// Gets the number of tiles actually placed in the grid.
        /// </summary>
        public int GetTileCount()
        {
            return tiles.Count;
        }


        /// <summary>
        /// Clears all tiles from the grid.
        /// </summary>
        public virtual void Clear()
        {
            tiles.Clear();
        }
    }
}