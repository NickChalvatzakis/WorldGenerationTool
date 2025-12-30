using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration
{
    public class VisualGrid : Grid<VisualTile>
    {
        private WorldGrid worldGrid;
        public Vector2 Offset { get; private set; }

        // The 4 neighbor offsets for a visual tile
        // |_0_|_1_|
        // |_2_|_3_|
        private static readonly Vector2Int[] NEIGHBOUR_OFFSETS = new Vector2Int[]
        {
            new Vector2Int(0, 0),   // Bottom-left
            new Vector2Int(1, 0),   // Bottom-right
            new Vector2Int(0, 1),   // Top-left
            new Vector2Int(1, 1)    // Top-right
        };

        public VisualGrid(int width, int height, WorldGrid worldGrid) : base(width, height)
        {
            this.worldGrid = worldGrid;
            Offset = new Vector2(0.5f, 0.5f);
            
            InitializeVisualCells();
        }

        /// <summary>
        /// Creates VisualTile instances for the entire grid.
        /// </summary>
        private void InitializeVisualCells()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    VisualTile tile = new VisualTile(x, y);
                    SetTile(x, y, tile);
                }
            }
        }

        /// <summary>
        /// Updates a specific visual tile's configuration based on its 4 WorldGrid neighbors.
        /// </summary>
        public void UpdateVisualTile(int x, int y)
        {
            if (worldGrid == null)
            {
                Debug.LogWarning("VisualGrid: WorldGrid reference is null");
                return;
            }

            VisualTile tile = GetTile(x, y);
            if (tile == null)
                return;

            // Get the 4 overlapping world tile types
            TileType bottomLeft = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[0].x, y + NEIGHBOUR_OFFSETS[0].y);
            TileType bottomRight = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[1].x, y + NEIGHBOUR_OFFSETS[1].y);
            TileType topLeft = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[2].x, y + NEIGHBOUR_OFFSETS[2].y);
            TileType topRight = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[3].x, y + NEIGHBOUR_OFFSETS[3].y);

            // Calculate configuration using bit flags (0-15)
            // Any non-None tile is considered "filled"
            tile.ConfigurationIndex = CalculateConfiguration(bottomLeft, bottomRight, topLeft, topRight);
            
            // Update the visual (mesh/prefab)
            tile.UpdateVisual();
        }

        /// <summary>
        /// Calculates configuration index (0-15) based on which neighbors are filled.
        /// Uses bit flags: bit 0 = bottom-left, bit 1 = bottom-right, bit 2 = top-left, bit 3 = top-right
        /// </summary>
        private int CalculateConfiguration(TileType bottomLeft, TileType bottomRight, TileType topLeft, TileType topRight)
        {
            int config = 0;
            if (bottomLeft != TileType.None) config |= 1;   // Bit 0
            if (bottomRight != TileType.None) config |= 2;  // Bit 1
            if (topLeft != TileType.None) config |= 4;      // Bit 2
            if (topRight != TileType.None) config |= 8;     // Bit 3
            return config;
        }

        /// <summary>
        /// Updates all visual tiles in the grid.
        /// </summary>
        public void UpdateAllVisuals()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    UpdateVisualTile(x, y);
                }
            }
        }

        /// <summary>
        /// Converts grid coordinates to world position with offset applied.
        /// </summary>
        public Vector3 GetWorldPosition(int x, int y, float tileSize = 1f)
        {
            return new Vector3(
                (x + Offset.x) * tileSize,
                0f,
                (y + Offset.y) * tileSize
            );
        }

        /// <summary>
        /// Clears all visual tiles and destroys their GameObjects.
        /// </summary>
        public override void Clear()
        {
            foreach (var tile in GetAllTiles())
            {
                tile?.DestroyVisual();
            }
            base.Clear();
        }
    }


}