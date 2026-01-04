using System;
using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Data.Layers;
using UnityEngine;

namespace CozyWorldGeneration.Core.DualGrid
{
    public class VisualGrid : Grid<VisualTile>
    {
        private WorldGrid worldGrid;
        private WorldLayer worldLayer;
        private int level;
        public Vector2 Offset { get; private set; }
        public Transform TilesContainer { get; set; }
        public float TileSize { get; set; }

        // The 4 neighbour offsets for a visual tile
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

        public VisualGrid(int width, int height, WorldGrid worldGrid, WorldLayer worldLayer, float tileSize = 1f) :
            base(width, height)
        {
            this.worldGrid = worldGrid;
            this.worldLayer = worldLayer;
            level = worldLayer?.LayerLevel ?? 0; // ADD THIS
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

            var bottomLeft =
                worldGrid.HasTileForLayer(x + NEIGHBOUR_OFFSETS[0].x, y + NEIGHBOUR_OFFSETS[0].y, worldLayer);
            var bottomRight =
                worldGrid.HasTileForLayer(x + NEIGHBOUR_OFFSETS[1].x, y + NEIGHBOUR_OFFSETS[1].y, worldLayer);
            var topLeft = worldGrid.HasTileForLayer(x + NEIGHBOUR_OFFSETS[2].x, y + NEIGHBOUR_OFFSETS[2].y, worldLayer);
            var topRight =
                worldGrid.HasTileForLayer(x + NEIGHBOUR_OFFSETS[3].x, y + NEIGHBOUR_OFFSETS[3].y, worldLayer);

            tile.ConfigurationIndex = CalculateConfiguration(bottomLeft, bottomRight, topLeft, topRight);

            // Debug: Log when we have a non-zero config
            if (tile.ConfigurationIndex > 0)
            {
                var visualLayer = GetVisualLayerForWorldLayer?.Invoke(worldLayer);
                Debug.Log($"[VisualGrid] Tile ({x},{y}) config: {tile.ConfigurationIndex}, " +
                          $"VisualLayer: {visualLayer?.LayerName ?? "NULL"}, " +
                          $"HasTileset: {visualLayer?.GetRandomTileset() != null}");
            }

            if (GetVisualLayerForWorldLayer != null && worldLayer != null)
            {
                var visualLayer = GetVisualLayerForWorldLayer(worldLayer);
                if (visualLayer != null) tile.SetVisualLayer(visualLayer);
            }

            if (TilesContainer != null)
                tile.UpdateVisual(TilesContainer, TileSize);
        }


        /// <summary>
        /// Calculates configuration index (0-15) based on which neighbors are filled.
        /// </summary>
        private int CalculateConfiguration(bool bottomLeft, bool bottomRight, bool topLeft, bool topRight)
        {
            var config = 0;
            if (bottomLeft) config |= 1;
            if (bottomRight) config |= 2;
            if (topLeft) config |= 4;
            if (topRight) config |= 8;
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