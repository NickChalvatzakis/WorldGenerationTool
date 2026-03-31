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

        /// <summary>
        /// Returns true if this position has solid support directly below it,
        /// or is at grid level 0 (the conceptual ground floor).
        /// </summary>
        public bool HasSolidBelow(int x, int y, int level)
        {
            return level == 0 || HasSolidTile(x, y, level - 1);
        }

        /// <summary>
        /// Scans the column at (x, y) downward from fromLevel to find the first solid tile.
        /// Returns solidLevel + 1 (the first empty level above that solid), or -1 if no solid found.
        /// </summary>
        public int FindLandingLevel(int x, int y, int fromLevel)
        {
            if (fromLevel < 0) return -1;
            for (var scanLevel = fromLevel; scanLevel >= 0; scanLevel--)
                if (HasSolidTile(x, y, scanLevel))
                    return scanLevel + 1;
            return -1;
        }

        #endregion

        #region Fluids

        public void PlaceFluid(int x, int y, int level, FluidType type, int fillLevel)
        {
            if (!IsValidPosition(x, y, level)) return;

            var tile = GetTile(x, y, level);

            if (tile == null)
            {
                tile = new WorldTile(x, y, null);
                SetTile(x, y, level, tile);
            }

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

            if (!tile.IsSolid) SetTile(x, y, level, null);

            if (!SuppressEvents)
                ToolEvents.TriggerFluidRemoved(new Vector3Int(x, y, level));
        }

        public bool HasFluid(int x, int y, int level)
        {
            var tile = GetTile(x, y, level);
            return tile != null && tile.HasFluid;
        }

        /// <summary>
        /// Returns the horizontal neighbours at the same level that fluid can spread into:
        /// they must be empty (no solid, no fluid) and must have solid support below them.
        /// Downward spread is handled separately by ApplyGravity / CreateWaterfalls.
        /// </summary>
        public List<Vector3Int> GetFluidSpreadPositions(int x, int y, int level)
        {
            var result = new List<Vector3Int>(4);
            Vector3Int[] horizontal =
            {
                new(x, y + 1, level),
                new(x + 1, y, level),
                new(x, y - 1, level),
                new(x - 1, y, level)
            };

            foreach (var pos in horizontal)
            {
                if (!IsValidPosition(pos.x, pos.y)) continue;
                if (HasFluid(pos.x, pos.y, pos.z)) continue;
                if (HasSolidTile(pos.x, pos.y, pos.z)) continue;
                if (!HasSolidBelow(pos.x, pos.y, pos.z)) continue;
                result.Add(pos);
            }

            return result;
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