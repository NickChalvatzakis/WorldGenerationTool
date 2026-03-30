using CozyWorldGeneration.Data.Layers;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CozyWorldGeneration.Core.Events;
using CozyWorldGeneration.Core.Fluids;
using CozyWorldGeneration.Data.Fluids;

namespace CozyWorldGeneration.Core.DualGrid
{
    public class WorldGrid : Grid3D<WorldTile>
    {
        public WorldGrid(int width, int height, int maxLevels) : base(width, height, maxLevels)
        {
        }

        #region Solid Tiles

        public void PlaceTile(int x, int y, WorldLayer layer)
        {
            if (!IsValidPosition(x, y) || layer == null) return;

            var tile = new WorldTile(x, y, layer);
            SetTile(x, y, layer.LayerLevel, tile);

            if (!SuppressEvents)
                ToolEvents.TriggerTileChanged(x, y);
        }

        public void RemoveTile(int x, int y, int level)
        {
            if (!HasTile(x, y, level)) return;
            SetTile(x, y, level, null);
            if (!SuppressEvents)
                ToolEvents.TriggerTileChanged(x, y);
        }

        public bool HasTileAt(int x, int y, int level)
        {
            return GetTile(x, y, level) != null;
        }

        /// <summary>
        /// Checks if a specific layer has a tile at this position.
        /// </summary>
        public bool HasTileForLayer(int x, int y, WorldLayer layer)
        {
            if (layer == null) return false;

            var tile = GetTile(x, y, layer.LayerLevel);
            return tile != null && tile.SourceLayer == layer;
        }

        public bool HasSolidTile(int x, int y, int level)
        {
            var tile = GetTile(x, y, level);
            return tile != null && tile.IsSolid;
        }

        #endregion

        #region Fluids

        public void PlaceFluid(int x, int y, int level, FluidType type, int fillLevel)
        {
            if (!IsValidPosition(x, y, level)) return;

            var tile = GetTile(x, y, level);

            // Create tile if it doesn't exist
            if (tile == null)
            {
                tile = new WorldTile(x, y, null);
                SetTile(x, y, level, tile);
            }

            // Create or update fluid data
            if (tile.Fluid == null)
            {
                tile.Fluid = new FluidData(type, fillLevel);
            }
            else
            {
                tile.Fluid.AddFillAmount(fillLevel);
                tile.Fluid.IsSettled = false;
            }

            if (!SuppressEvents)
                ToolEvents.TriggerFluidPlaced(tile);
        }

        public void RemoveFluid(int x, int y, int level)
        {
            var tile = GetTile(x, y, level);
            if (tile == null || tile.Fluid == null) return;

            tile.Fluid = null;

            // If tile has no solid layer either, remove it completely
            if (!tile.IsSolid) SetTile(x, y, level, null);

            if (!SuppressEvents)
                ToolEvents.TriggerFluidRemoved(new Vector3Int(x, y, level));
        }

        public bool HasFluid(int x, int y, int level)
        {
            var tile = GetTile(x, y, level);
            return tile != null && tile.HasFluid;
        }

        public List<Vector3Int> GetFluidSpreadPositions(int x, int y, int level)
        {
            var neighbourPositions = GetAllCardinalNeighbours(x, y, level);
            var spreadablePositions = neighbourPositions
                .Where(position => IsValidPosition(position))
                .Where(position => position.z <= level)
                .Where(position => !HasFluid(position.x, position.y, position.z))
                .Where(position => !HasSolidTile(position.x, position.y, position.z))
                .ToList();

            return spreadablePositions;
        }

        public IEnumerable<WorldTile> GetAllFluidTiles()
        {
            return GetAllTiles().Where(tile => tile.HasFluid);
        }

        public IEnumerable<WorldTile> GetFluidTilesAtLevel(int level)
        {
            return GetTilesAtLevel(level).Where(tile => tile.HasFluid);
        }

        #endregion


        public override void Clear()
        {
            base.Clear();
            if (!SuppressEvents)
                ToolEvents.TriggerGridCleared();
        }
    }
}