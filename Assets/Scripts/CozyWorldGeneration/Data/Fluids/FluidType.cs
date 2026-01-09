using UnityEngine;

namespace CozyWorldGeneration.Data.Fluids
{
    [CreateAssetMenu(fileName = "NewFluidType", menuName = "Cozy World Generation/Fluid Type")]
    public class FluidType : ScriptableObject
    {
        [SerializeField] private string guid;
        [SerializeField] private string fluidName;
        [SerializeField] private int spreadRate;
        [SerializeField] private bool canSettle;
        [SerializeField] private int settlingThreshold;
        [SerializeField] private Color color;
        [SerializeField] private Material material;

        public string GUID
        {
            get => guid;
            set => guid = value;
        }

        public string FluidName
        {
            get => fluidName;
            set => fluidName = value;
        }

        public int SpreadRate
        {
            get => spreadRate;
            set => spreadRate = value;
        }

        public bool CanSettle
        {
            get => canSettle;
            set => canSettle = value;
        }

        public int SettlingThreshold
        {
            get => settlingThreshold;
            set => settlingThreshold = value;
        }

        public Color Color
        {
            get => color;
            set => color = value;
        }

        public Material Material
        {
            get => material;
            set => material = value;
        }
    }
}