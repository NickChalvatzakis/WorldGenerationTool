using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration
{
    public class VisualGrid : Grid<VisualTile>
    {
        private WorldGrid worldGrid;

        // We offset the Visual Grid from the main Grid so we can check more overlapping neighbours, 
        // making our tile count 16 instead of 256
        public Vector2 Offset { get; private set; }

        private static readonly Vector2Int[] NEIGHBOUR_OFFSETS = new Vector2Int[]
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        //  https://github.com/jess-hammer/dual-grid-tilemap-system-unity/blob/main/Assets/Scripts/DualGridTilemap.cs

        // The 4 neighbour offsets for a visual cell
        // |_0_|_1_|
        // |_2_|_3_|
        private static Dictionary<Tuple<TileType, TileType, TileType, TileType>, int> configurationLookup;

        public VisualGrid(int width, int height, WorldGrid worldGrid) : base(width, height)
        {
            this.worldGrid = worldGrid;
            Offset = new Vector2(0.5f, 0.5f);

            InitializeVisualTiles();
        }

        // private void InitializeConfigurationLookup()
        // {
        //     var N = TileType.None;
        //     var G = TileType.Grass;
        //
        //     configurationLookup = new Dictionary<Tuple<TileType, TileType, TileType, TileType>, int>()
        //     {
        //         // Empty
        //         { new(N, N, N, N), 0 },
        //
        //         // Single Corners
        //         { new(N, N, N, G), 1 },
        //         { new(N, N, G, N), 2 },
        //         { new(N, G, N, N), 3 },
        //         { new(G, N, N, N), 4 },
        //         
        //         // Edges
        //         { new(N, N, G, G), 5 },
        //         { new(G, N, G, N), 6 },
        //         { new(N, G, N, G), 7 },
        //         { new(G, G, N, N), 8 },
        //         
        //         // Corners
        //         { new(G, N, N, G), 9 },
        //         { new(N, G, G, N), 10 },
        //         
        //         // Three Filled
        //         { new(G, N, G, G), 11 },
        //         { new(N, G, G, G), 12 },
        //         { new(G, G, G, N), 13 },
        //         { new(G, G, N, G), 14 },
        //         
        //         // All Filled
        //         { new(G, G, G, G), 15 },
        //
        //     };
        // }

        private void InitializeVisualTiles()
        {
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
            {
                var visualTile = new VisualTile(x, y, worldGrid);
                SetTile(x, y, visualTile);
            }
        }

        public void UpdateVisualTile(int x, int y)
        {
            var visualTile = GetTile(x, y);
            if(visualTile == null) return;
            
            TileType bottomLeft = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[0].x, y + NEIGHBOUR_OFFSETS[0].y);
            TileType bottomRight = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[1].x, y + NEIGHBOUR_OFFSETS[1].y);
            TileType topLeft = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[2].x, y + NEIGHBOUR_OFFSETS[2].y);
            TileType topRight = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[3].x, y + NEIGHBOUR_OFFSETS[3].y);
            
            visualTile.ConfigurationIndex = CalculateConfiguration(bottomLeft, bottomRight, topLeft, topRight);
            visualTile.UpdateVisual();
 
        }
        
        // https://dev.to/joestrout/wang-2-corner-tiles-544k
        private int CalculateConfiguration(TileType bottomLeft, TileType bottomRight, TileType topLeft, TileType topRight)
        {
            int config = 0;
            if (bottomLeft != TileType.None) config |= 1;
            if (bottomRight != TileType.None) config |= 2;
            if (topLeft != TileType.None) config |= 4;   
            if (topRight != TileType.None) config |= 8;
            return config;
        }

        public void UpdateAllVisualTiles()
        {
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
                UpdateVisualTile(x, y);
        }

        public Vector3 GetWorldPosition(int x, int y, float tileSize = 1f)
        {
            return new Vector3(
                (x + Offset.x) * tileSize,
                0f, // TODO: Will have to change to layer Index
                (y + Offset.y) * tileSize);
        }

        public override void Clear()
        {
            foreach (var visualTile in GetAllTiles()) visualTile.DestroyVisual();
            base.Clear();
        }
    }
}