using CozyWorldGeneration.Data.Fluids;
using UnityEngine;

namespace CozyWorldGeneration.Core.Fluids
{
    public class FluidData
    {
        public FluidData(FluidType type, int fillAmount = 0)
        {
            Type = type;
            FillAmount = Mathf.Clamp(fillAmount, 0, 7);
            BodyId = -1;
            IsSource = false;
            IsSettled = false;
            IsWaterfall = false;
            FlowDirection = Vector2.zero;
        }

        public FluidType Type { get; set; }
        public int FillAmount { get; set; }
        public bool IsSource { get; set; }
        public Vector2 FlowDirection { get; set; }
        public int BodyId { get; set; }
        public bool IsSettled { get; set; }

        /// <summary>
        /// True for tiles that are part of a falling fluid column.
        /// Waterfall tiles are intentionally unsupported (no solid below) and are
        /// skipped by ApplyGravity and SpreadBodies so they stay in place.
        /// </summary>
        public bool IsWaterfall { get; set; }

        public float FillLevel => FillAmount / 7f;
        public bool IsEmpty => FillAmount == 0;
        public bool IsFull => FillAmount == 7;

        public void AddFillAmount(int amount)
        {
            FillAmount = Mathf.Clamp(FillAmount + amount, 0, 7);
        }

        public void RemoveFillAmount(int amount)
        {
            FillAmount = Mathf.Clamp(FillAmount - amount, 0, 7);
        }
    }
}