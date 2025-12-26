using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration.Layers
{
    [Serializable]
    public class LayerCollection
    {
        [SerializeField] private string guid;
        [SerializeField] private string collectionName;
        [SerializeField] private List<string> assignedWorldLayers = new List<string>();
        [SerializeField] private List<WorldLayer> layers = new List<WorldLayer>();

        public bool foldoutState = true;
        
        public string GUID { get => guid; set => guid = value; }
        public string CollectionName { get => collectionName; set => collectionName = value; }
        public List<string> AssignedWorldLayers { get => assignedWorldLayers; set => assignedWorldLayers = value; }
        public List<WorldLayer> Layers { get => layers; set => layers = value; }

        public LayerCollection(string name)
        {
            collectionName = name;
            guid = System.Guid.NewGuid().ToString();
        }
        
        public void AddLayer(WorldLayer layer)
        {
            if (layer != null && !layers.Contains(layer))
            {
                layers.Add(layer);
                assignedWorldLayers.Add(layer.GUID);
            }
        }
        
        public void RemoveLayer(WorldLayer layer)
        {
            if (layer != null)
            {
                layers.Remove(layer);
                assignedWorldLayers.Remove(layer.GUID);
            }
        }

        public WorldLayer GetLayerByGUID(string guid)
        {
            return layers.Find(l => l.GUID == guid);
        }
        
        public List<WorldLayer> GetEnabledLayers()
        {
            return layers.FindAll(l => l.IsEnabled);
        }


    }
}