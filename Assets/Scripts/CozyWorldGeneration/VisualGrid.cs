using System;
using System.Collections.Generic;
using CozyWorldGeneration.Data.Layers;
using UnityEngine;

namespace CozyWorldGeneration
{
    public class VisualGrid : Grid<VisualTile>
    {
        private WorldGrid worldGrid;
        public Vector2 Offset { get; private set; }
        public Transform TilesContainer { get; set; }
        public float TileSize { get; set; }

        // The 4 neighbor offsets for a visual tile
        // |_0_|_1_|
        // |_2_|_3_|
        private static readonly Vector2Int[] NEIGHBOUR_OFFSETS = new Vector2Int[]
        {
            new(0, 0), // Bottom-left
            new(1, 0), // Bottom-right
            new(0, 1), // Top-left
            new(1, 1) // Top-right
        };

        // Delegate to get VisualLayer for a WorldLayer (set by GridManager)
        public Func<WorldLayer, VisualLayer> GetVisualLayerForWorldLayer { get; set; }

        public VisualGrid(int width, int height, WorldGrid worldGrid, float tileSize = 1f) : base(width, height)
        {
            this.worldGrid = worldGrid;
            TileSize = tileSize;
            Offset = new Vector2(0.5f, 0.5f);

            InitializeVisualCells();
        }

        /// <summary>
        /// Creates VisualTile instances for the entire grid.
        /// </summary>
        private void InitializeVisualCells()
        {
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
            {
                var tile = new VisualTile(x, y);
                SetTile(x, y, tile);
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

            var tile = GetTile(x, y);
            if (tile == null)
                return;

            // Get the 4 overlapping world tile types
            var bottomLeft = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[0].x, y + NEIGHBOUR_OFFSETS[0].y);
            var bottomRight = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[1].x, y + NEIGHBOUR_OFFSETS[1].y);
            var topLeft = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[2].x, y + NEIGHBOUR_OFFSETS[2].y);
            var topRight = worldGrid.GetTileTypeAt(x + NEIGHBOUR_OFFSETS[3].x, y + NEIGHBOUR_OFFSETS[3].y);

            // Calculate configuration using bit flags (0-15)
            tile.ConfigurationIndex = CalculateConfiguration(bottomLeft, bottomRight, topLeft, topRight);

            // Find the dominant WorldLayer to determine which VisualLayer to use
            var dominantWorldLayer = GetDominantWorldLayer(x, y);

            // Get the corresponding VisualLayer using the delegate (set by GridManager)
            if (GetVisualLayerForWorldLayer != null && dominantWorldLayer != null)
            {
                var visualLayer = GetVisualLayerForWorldLayer(dominantWorldLayer);
                if (visualLayer != null) tile.SetVisualLayer(visualLayer);
            }

            // Update the visual (mesh/prefab) if container is set
            if (TilesContainer != null) tile.UpdateVisual(TilesContainer, TileSize);
        }

        /// <summary>
        /// Gets the dominant WorldLayer from the 4 overlapping tiles.
        /// </summary>
        private WorldLayer GetDominantWorldLayer(int x, int y)
        {
            var overlappingTiles = new WorldTile[4]
            {
                worldGrid.GetTile(x + NEIGHBOUR_OFFSETS[0].x, y + NEIGHBOUR_OFFSETS[0].y),
                worldGrid.GetTile(x + NEIGHBOUR_OFFSETS[1].x, y + NEIGHBOUR_OFFSETS[1].y),
                worldGrid.GetTile(x + NEIGHBOUR_OFFSETS[2].x, y + NEIGHBOUR_OFFSETS[2].y),
                worldGrid.GetTile(x + NEIGHBOUR_OFFSETS[3].x, y + NEIGHBOUR_OFFSETS[3].y)
            };

            foreach (var tile in overlappingTiles)
                if (tile != null && tile.SourceLayer != null)
                    return tile.SourceLayer;

            return null;
        }

        /// <summary>
        /// Calculates configuration index (0-15) based on which neighbors are filled.
        /// </summary>
        private int CalculateConfiguration(TileType bottomLeft, TileType bottomRight, TileType topLeft,
            TileType topRight)
        {
            var config = 0;
            if (bottomLeft != TileType.None) config |= 1;
            if (bottomRight != TileType.None) config |= 2;
            if (topLeft != TileType.None) config |= 4;
            if (topRight != TileType.None) config |= 8;
            return config;
        }

        /// <summary>
        /// Updates all visual tiles in the grid.
        /// </summary>
        public void UpdateAllVisuals()
        {
            for (var x = 0; x < Width; x++)
            for (var y = 0; y < Height; y++)
                UpdateVisualTile(x, y);
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
            foreach (var tile in GetAllTiles()) tile?.DestroyVisual();
            base.Clear();
        }
    }
}