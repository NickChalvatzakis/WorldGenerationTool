using UnityEngine;
using System;
using System.Collections.Generic;
using CozyWorldGeneration.Events;

namespace CozyWorldGeneration.Layers
{
    /// <summary>
    /// Serializable collection of WorldLayers.
    /// Used for both World and Visual layer groups.
    /// </summary>
    [Serializable]
    public class LayerCollection
    {
        [SerializeField] private string guid = Guid.NewGuid().ToString();
        [SerializeField] private string collectionName = "Layer Collection";
        [SerializeField] private List<string> assignedWorldLayers = new();
        [SerializeField] private List<WorldLayer> layers = new();

        // Non-serialized UI state
        [NonSerialized] public bool foldoutState = true;

        public string GUID
        {
            get => guid;
            private set => guid = value;
        }

        public string CollectionName
        {
            get => collectionName;
            set => collectionName = value;
        }

        public List<string> AssignedWorldLayers
        {
            get => assignedWorldLayers;
            set => assignedWorldLayers = value;
        }

        public List<WorldLayer> Layers
        {
            get => layers;
            set => layers = value;
        }

        public LayerCollection(string name)
        {
            collectionName = name;
            guid = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Adds a layer to this collection.
        /// </summary>
        public void AddLayer(WorldLayer layer)
        {
            if (layer != null && !layers.Contains(layer))
            {
                layers.Add(layer);
                assignedWorldLayers.Add(layer.GUID);
                ToolEvents.RaiseLayerAdded(layer);
            }
        }

        /// <summary>
        /// Removes a layer from this collection.
        /// </summary>
        public void RemoveLayer(WorldLayer layer)
        {
            if (layer != null)
            {
                layers.Remove(layer);
                assignedWorldLayers.Remove(layer.GUID);
                ToolEvents.RaiseLayerRemoved(layer);
            }
        }

        /// <summary>
        /// Gets a layer by its GUID.
        /// </summary>
        public WorldLayer GetLayerByGUID(string guid)
        {
            return layers.Find(l => l.GUID == guid);
        }

        /// <summary>
        /// Gets all enabled layers.
        /// </summary>
        public List<WorldLayer> GetEnabledLayers()
        {
            return layers.FindAll(l => l.IsEnabled);
        }
    }
}