using System.Collections.Generic;
using CozyWorldGeneration.Data.Tilesets;
using UnityEngine;

namespace CozyWorldGeneration.Data.Layers
{
    [CreateAssetMenu(fileName = "NewVisualLayer", menuName = "Cozy World Generation/Visual Layer")]
    public class VisualLayer : ScriptableObject
    {
        [SerializeField] private string guid = System.Guid.NewGuid().ToString();
        [SerializeField] private string layerName = "New Visual Layer";
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private bool isFluidLayer = false;

        [Header("World Layer Reference")] [Tooltip("Which World Layer this visual layer represents")] [SerializeField]
        private WorldLayer assignedWorldLayer;

        [Header("Tilesets")] [Tooltip("Tilesets with weights for random selection")] [SerializeField]
        private List<WeightedTileset> tilesets = new();


        [Header("Settings")] [SerializeField] private float visualHeight = 0;

        [System.NonSerialized] public bool foldoutState = false;

        public bool IsFluidLayer
        {
            get => isFluidLayer;
            set => isFluidLayer = value;
        }


        public string GUID
        {
            get => guid;
            private set => guid = value;
        }

        public string LayerName
        {
            get => layerName;
            set => layerName = value;
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        public WorldLayer AssignedWorldLayer
        {
            get => assignedWorldLayer;
            set => assignedWorldLayer = value;
        }

        public List<WeightedTileset> Tilesets => tilesets ??= new List<WeightedTileset>();


        public float VisualHeight
        {
            get => visualHeight;
            set => visualHeight = value;
        }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(guid)) guid = System.Guid.NewGuid().ToString();
        }

        public Tileset GetRandomTileset()
        {
            if (tilesets == null || tilesets.Count == 0)
                return null;

            // Calculate total weight
            var totalWeight = 0f;
            foreach (var weighted in tilesets)
                if (weighted.tileset != null)
                    totalWeight += weighted.weight;

            if (totalWeight <= 0)
                return tilesets[0]?.tileset;

            // Random selection based on weight
            var randomValue = Random.Range(0f, totalWeight);
            var currentWeight = 0f;

            foreach (var weighted in tilesets)
            {
                if (weighted.tileset == null)
                    continue;

                currentWeight += weighted.weight;
                if (randomValue <= currentWeight) return weighted.tileset;
            }

            return tilesets[0]?.tileset;
        }

        public void AddTileset(Tileset tileset, float weight = 1f)
        {
            tilesets.Add(new WeightedTileset { tileset = tileset, weight = weight });
        }

        public void RemoveTileset(Tileset tileset)
        {
            tilesets.RemoveAll(w => w.tileset == tileset);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Assigns a fresh GUID. Called automatically by LayerGuidValidator when a
        /// duplicate is detected (e.g. after copy-pasting this asset in the Project window).
        /// </summary>
        public void RegenerateGuid()
        {
            guid = System.Guid.NewGuid().ToString();
        }
#endif
    }

    [System.Serializable]
    public class WeightedTileset
    {
        public Tileset tileset;
        [Range(0.1f, 1f)] public float weight = 1f;
    }
}