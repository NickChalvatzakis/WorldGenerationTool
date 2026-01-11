using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CozyWorldGeneration.Data.Layers
{
    [CreateAssetMenu(fileName = "NewVisualLayerCollection", menuName = "Cozy World Generation/Visual Layer Collection")]
    public class VisualLayerCollection : ScriptableObject
    {
        [SerializeField] private string collectionName = "Visual Layers";
        [SerializeField] private List<VisualLayer> visualLayers = new();

        public string CollectionName
        {
            get => collectionName;
            set => collectionName = value;
        }

        public List<VisualLayer> Layers => visualLayers;

        public VisualLayerCollection(string name)
        {
            collectionName = name;
            visualLayers = new List<VisualLayer>();
        }

        public VisualLayer GetVisualLayerForWorldLayer(WorldLayer worldLayer)
        {
            if (worldLayer == null) return null;

            foreach (var visualLayer in visualLayers)
            {
                if (visualLayer == null) continue;
                if (!visualLayer.IsEnabled) continue;
                if (visualLayer.IsFluidLayer) continue; // Skip fluid layers

                if (visualLayer.AssignedWorldLayer == worldLayer)
                    return visualLayer;
            }

            return null;
        }

        public VisualLayer GetFluidVisualLayer()
        {
            return visualLayers.FirstOrDefault(layer =>
                layer != null &&
                layer.IsEnabled &&
                layer.IsFluidLayer);
        }

        public List<VisualLayer> GetAllFluidVisualLayers()
        {
            return visualLayers
                .Where(layer => layer != null && layer.IsEnabled && layer.IsFluidLayer)
                .ToList();
        }

        public void AddLayer(VisualLayer layer)
        {
            if (layer != null && !visualLayers.Contains(layer))
                visualLayers.Add(layer);
        }

        public void RemoveLayer(VisualLayer layer)
        {
            visualLayers.Remove(layer);
        }

        public void Clear()
        {
            visualLayers.Clear();
        }
    }
}