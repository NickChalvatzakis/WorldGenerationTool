using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyWorldGeneration.Core
{
    /// <summary>
    /// Generic sparse 2D grid base class. Uses Dictionary for efficient storage.
    /// </summary>
    /// <typeparam name="T">The tile type. Must be a reference type.</typeparam>
    public abstract class Grid<T> where T : class
    {
        protected Dictionary<Vector2Int, T> tiles;

        public int Width { get; protected set; }
        public int Height { get; protected set; }

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Dictionary<Vector2Int, T>();
        }

        public T GetTile(int x, int y)
        {
            var key = new Vector2Int(x, y);
            tiles.TryGetValue(key, out var tile);
            return tile;
        }

        public T GetTile(Vector2Int position)
        {
            tiles.TryGetValue(position, out var tile);
            return tile;
        }

        public virtual void SetTile(int x, int y, T tile)
        {
            var key = new Vector2Int(x, y);

            if (tile == null)
                tiles.Remove(key);
            else
                tiles[key] = tile;
        }

        public void SetTile(Vector2Int position, T tile)
        {
            SetTile(position.x, position.y, tile);
        }

        public bool IsValidPosition(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsValidPosition(Vector2Int position)
        {
            return IsValidPosition(position.x, position.y);
        }

        public bool HasTile(int x, int y)
        {
            return tiles.ContainsKey(new Vector2Int(x, y));
        }

        public bool HasTile(Vector2Int position)
        {
            return tiles.ContainsKey(position);
        }

        public List<T> GetCardinalNeighbours(int x, int y)
        {
            var neighbours = new List<T>(4);

            Vector2Int[] directions =
            {
                new(0, 1),
                new(1, 0),
                new(0, -1),
                new(-1, 0)
            };

            neighbours.AddRange(from dir in directions
                let nx = x + dir.x
                let ny = y + dir.y
                select GetTile(nx, ny)
                into neighbour
                where neighbour != null
                select neighbour);

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