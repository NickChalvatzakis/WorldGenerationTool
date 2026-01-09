using UnityEngine;
using System;
using System.Collections.Generic;
using CozyWorldGeneration.Core.Events;
using CozyWorldGeneration.Data.Layers;

namespace CozyWorldGeneration
{
    /// <summary>
    /// Abstract base class for layer collections.
    /// Contains shared properties and behavior.
    /// </summary>
    [Serializable]
    public abstract class LayerCollection
    {
        [SerializeField] private string guid = Guid.NewGuid().ToString();
        [SerializeField] private string collectionName = "Layer Collection";

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

        protected LayerCollection(string name)
        {
            collectionName = name;
            guid = Guid.NewGuid().ToString();
        }
    }

    /// <summary>
    /// Collection specifically for WorldLayers.
    /// </summary>
    [Serializable]
    public class WorldLayerCollection : LayerCollection
    {
        [SerializeField] private List<WorldLayer> layers = new();

        public List<WorldLayer> Layers
        {
            get => layers;
            set => layers = value;
        }

        public WorldLayerCollection(string name) : base(name)
        {
        }

        /// <summary>
        /// Adds a WorldLayer to this collection.
        /// </summary>
        public void AddLayer(WorldLayer layer)
        {
            if (layer != null && !layers.Contains(layer))
            {
                layers.Add(layer);
                ToolEvents.TriggerLayerAdded(layer);
            }
        }

        /// <summary>
        /// Removes a WorldLayer from this collection.
        /// </summary>
        public void RemoveLayer(WorldLayer layer)
        {
            if (layer != null)
            {
                layers.Remove(layer);
                ToolEvents.TriggerLayerRemoved(layer);
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

    /// <summary>
    /// Collection specifically for VisualLayers.
    /// </summary>
    [Serializable]
    public class VisualLayerCollection : LayerCollection
    {
        [SerializeField] private List<VisualLayer> layers = new();

        public List<VisualLayer> Layers
        {
            get => layers;
            set => layers = value;
        }

        public VisualLayerCollection(string name) : base(name)
        {
        }

        /// <summary>
        /// Adds a VisualLayer to this collection.
        /// </summary>
        public void AddLayer(VisualLayer layer)
        {
            if (layer != null && !layers.Contains(layer)) layers.Add(layer);
        }

        /// <summary>
        /// Removes a VisualLayer from this collection.
        /// </summary>
        public void RemoveLayer(VisualLayer layer)
        {
            if (layer != null) layers.Remove(layer);
        }

        /// <summary>
        /// Gets a layer by its GUID.
        /// </summary>
        public VisualLayer GetLayerByGUID(string guid)
        {
            return layers.Find(l => l.GUID == guid);
        }

        /// <summary>
        /// Gets all enabled layers.
        /// </summary>
        public List<VisualLayer> GetEnabledLayers()
        {
            return layers.FindAll(l => l.IsEnabled);
        }

        /// <summary>
        /// Finds a VisualLayer assigned to a specific WorldLayer.
        /// </summary>
        public VisualLayer GetVisualLayerForWorldLayer(WorldLayer worldLayer)
        {
            if (worldLayer == null)
                return null;

            return layers.Find(l => l.AssignedWorldLayer == worldLayer);
        }
    }
}