using CozyWorldGeneration.Data.Fluids;
using UnityEngine;

namespace CozyWorldGeneration.Core.Fluids
{
    public class FluidTile
    {
        public FluidTile(Vector2Int position, FluidType type, int fillAmount = 0)
        {
            GridPosition = position;
            Type = type;
            FillAmount = fillAmount;
            BodyId = -1;
        }

        public Vector2Int GridPosition { get; set; }
        public FluidType Type { get; set; }
        public int FillAmount { get; set; }
        public bool IsSource { get; set; }
        public Vector2 FlowDirection { get; set; }
        public int BodyId { get; set; }
        public bool IsSettled { get; set; }

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