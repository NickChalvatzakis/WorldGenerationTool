using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration
{
    public abstract class Grid<T> where T : class
    {
        protected Dictionary<Vector2Int, T> tiles;
        
        public int Width {get; protected set;}
        public int Height {get; protected set;}

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            tiles = new Dictionary<Vector2Int, T>();
        }

        public T GetTile(Vector2Int position)
        {
            tiles.TryGetValue(position, out T tile);
            return tile;
        }

        public virtual void SetTile(int x, int y, T tile)
        {
            var key = new Vector2Int(x, y);
            if (tile == null)
            {
                tiles.Remove(key);
            }
            else
            {
                tiles[key] = tile;
            }
        }
        
        
        
        
    }
    
}