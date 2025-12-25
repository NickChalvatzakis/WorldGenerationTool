using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration
{
    public abstract class Grid<T> where T : class
    {
        protected Dictionary<Vector2Int, T> tiles;

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Dictionary<Vector2Int, T>();
        }

        public int Width { get; protected set; }
        public int Height { get; protected set; }

        public T GetTile(Vector2Int position)
        {
            tiles.TryGetValue(position, out var tile);
            return tile;
        }

        public T GetTile(int x, int y)
        {
            return GetTile(new Vector2Int(x, y));
        }

        public virtual void SetTile(int x, int y, T tile)
        {
            var key = new Vector2Int(x, y);
            if (tile == null)
                tiles.Remove(key);
            else
                tiles[key] = tile;
        }

        private void SetTile(Vector2Int position, T tile)
        {
            SetTile(position.x, position.y, tile);
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return IsValidPosition(position.x, position.y);
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool HasTile(Vector2Int position)
        {
            return tiles.ContainsKey(position);
        }

        public bool HasTile(int x, int y)
        {
            return HasTile(new Vector2Int(x, y));
        }

        // Get the URDL neighbours
        public List<T> GetCardinalNeighbours(int x, int y)
        {
            var neighbours = new List<T>();
            Vector2Int[] directions =
            {
                new(0, 1), // Up
                new(1, 0), // Left
                new(0, -1), // Down
                new(1, 1) // Right
            };

            foreach (var dir in directions)
            {
                var nx = x + dir.x;
                var ny = y + dir.y;

                var neighbour = GetTile(nx, ny);

                if (neighbour != null) neighbours.Add(neighbour);
            }

            return neighbours;
        }

        public List<T> GetCardinalNeighbours(Vector2Int position)
        {
            return GetCardinalNeighbours(position.x, position.y);
        }

        public IEnumerable<T> GetAllTiles()
        {
            return tiles.Values;
        }

        public IEnumerable<Vector2Int> GetAllPositions()
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