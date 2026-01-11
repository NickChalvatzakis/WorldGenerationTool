using System.Collections.Generic;
using System.Linq;
using CozyWorldGeneration.Data.Fluids;
using UnityEngine;

namespace CozyWorldGeneration.Core.Fluids
{
    public class FluidBody
    {
        public FluidBody(int bodyId, FluidType type)
        {
            BodyId = bodyId;
            Type = type;
            Tiles = new Dictionary<Vector3Int, FluidData>();
        }


        public int BodyId { get; set; }
        public FluidType Type { get; set; }
        public Dictionary<Vector3Int, FluidData> Tiles { get; set; }


        /// <summary>
        /// Sum of all FillAmount across tiles in the body
        /// </summary>
        public int TotalVolume => Tiles.Sum(t => t.Value.FillAmount);

        /// <summary>
        /// TotalVolume divided by Tile Count
        /// </summary>
        public float AverageFillAmount => Tiles.Count > 0 ? TotalVolume / (float)Tiles.Count : 0;

        public int TotalCount => Tiles.Count;
        public bool HasSource => Tiles.Any(t => t.Value.IsSource);
        public bool IsSettled => Tiles.All(t => t.Value.IsSettled);
        public int LowestLevel => Tiles.Count > 0 ? Tiles.Min(p => p.Key.z) : 0;
        public int HighestLevel => Tiles.Count > 0 ? Tiles.Max(p => p.Key.z) : 0;

        public void AddTile(Vector3Int position, FluidData fluidData)
        {
            fluidData.BodyId = BodyId;
            Tiles.TryAdd(position, fluidData);
        }

        public List<FluidData> GetTilesAtLevel(int level)
        {
            return Tiles.Where(kvp => kvp.Key.z == level).Select(kvp => kvp.Value).ToList();
        }

        public List<Vector3Int> GetTilePositionsAtLevel(int level)
        {
            return Tiles.Where(kvp => kvp.Key.z == level).Select(kvp => kvp.Key).ToList();
        }

        public void Settle()
        {
            foreach (var fluidData in Tiles.Values)
                fluidData.IsSettled = true;
        }

        public void Unsettle()
        {
            foreach (var fluidData in Tiles.Values)
                fluidData.IsSettled = false;
        }
    }
}