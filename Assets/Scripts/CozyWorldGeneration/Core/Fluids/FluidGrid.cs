using System.Collections.Generic;
using System.Linq;
using CozyWorldGeneration.Core.DualGrid;
using CozyWorldGeneration.Core.Events;
using CozyWorldGeneration.Data.Fluids;
using UnityEngine;

namespace CozyWorldGeneration.Core.Fluids
{
    public class FluidGrid : Grid3D<FluidTile>
    {
        public FluidGrid(int width, int height, int maxLevels) : base(width, height, maxLevels)
        {
        }

        public void PlaceFluid(int x, int y, int level, FluidType type, int fillLevel)
        {
            if (!IsValidPosition(x, y, level)) return;

            var tile = HasTile(x, y, level)
                ? GetTile(x, y, level)
                : new FluidTile(new Vector2Int(x, y), type);
            tile.AddFillAmount(fillLevel);
            SetTile(x, y, level, tile);

            if (!SuppressEvents) ToolEvents.TriggerFluidPlaced(tile);
        }

        public void RemoveFluid(int x, int y, int level)
        {
            if (!HasTile(x, y, level)) return;
            SetTile(x, y, level, null);
            if (!SuppressEvents) ToolEvents.TriggerFluidRemoved(new Vector3Int(x, y, level));
        }

        /// <summary>
        /// Gets all empty neighbour positions and below
        /// </summary>
        public List<Vector3Int> GetSpreadPositions(int x, int y, int level, WorldGrid worldGrid)
        {
            var neighbourPositions = worldGrid.GetAllCardinalNeighbours(x, y, level);
            var spreadablePositions = neighbourPositions.Where(position => position.z <= level)
                .Where(position => !HasTile(position))
                .Where(position => !worldGrid.HasTileAt(position.x, position.y, position.z))
                .ToList();
            return spreadablePositions;
        }
    }
}