using System;
using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Data.Layers;
using UnityEngine;

namespace CozyWorldGeneration.Events
{
    /// <summary>
    /// Centralized event system for Cozy World Generation tool.
    /// All events related to layers, grids, and painting are managed here.
    /// </summary>
    public static class ToolEvents
    {
        #region Layer Events

        /// <summary>
        /// Fired when a layer is added to a collection.
        /// </summary>
        public static event Action<WorldLayer> OnLayerAdded;

        /// <summary>
        /// Fired when a layer is removed from a collection.
        /// </summary>
        public static event Action<WorldLayer> OnLayerRemoved;

        /// <summary>
        /// Fired when a layer is cleared (all painted data removed).
        /// </summary>
        public static event Action<WorldLayer> OnLayerCleared;

        #endregion

        #region Grid Events

        /// <summary>
        /// Fired when a tile is placed on the WorldGrid.
        /// Parameters: x, y, tileType, sourceLayer
        /// </summary>
        public static event Action<int, int, TileType, WorldLayer> OnTilePlaced;

        /// <summary>
        /// Fired when a tile is removed from the WorldGrid.
        /// Parameters: x, y
        /// </summary>
        public static event Action<int, int> OnTileRemoved;

        public static event Action<int, int> OnTileChanged;

        /// <summary>
        /// Fired when the grid is cleared.
        /// </summary>
        public static event Action OnGridCleared;

        /// <summary>
        /// Fired when the grid is initialized or reinitialized.
        /// Parameters: width, height
        /// </summary>
        public static event Action<int, int> OnGridInitialized;

        #endregion

        #region Painting Events

        /// <summary>
        /// Fired when painting starts.
        /// </summary>
        public static event Action<WorldLayer> OnPaintingStarted;

        /// <summary>
        /// Fired when painting stops.
        /// </summary>
        public static event Action OnPaintingStopped;

        /// <summary>
        /// Fired when the active painting layer changes.
        /// </summary>
        public static event Action<WorldLayer> OnActiveLayerChanged;

        #endregion

        #region Event Invocation Methods

        // Layer Events
        public static void TriggerLayerAdded(WorldLayer layer)
        {
            OnLayerAdded?.Invoke(layer);
        }

        public static void TriggerLayerRemoved(WorldLayer layer)
        {
            OnLayerRemoved?.Invoke(layer);
        }

        public static void TriggerLayerCleared(WorldLayer layer)
        {
            OnLayerCleared?.Invoke(layer);
        }


        // Grid Events
        public static void TriggerTilePlaced(int x, int y, TileType tileType, WorldLayer sourceLayer)
        {
            OnTilePlaced?.Invoke(x, y, tileType, sourceLayer);
        }

        public static void TriggerTileRemoved(int x, int y)
        {
            OnTileRemoved?.Invoke(x, y);
        }

        public static void TriggerTileChanged(int x, int y)
        {
            OnTileChanged?.Invoke(x, y);
        }

        public static void TriggerGridCleared()
        {
            OnGridCleared?.Invoke();
        }

        public static void TriggerGridInitialized(int width, int height)
        {
            OnGridInitialized?.Invoke(width, height);
        }

        // Painting Events
        public static void TriggerPaintingStarted(WorldLayer layer)
        {
            OnPaintingStarted?.Invoke(layer);
        }

        public static void TriggerPaintingStopped()
        {
            OnPaintingStopped?.Invoke();
        }

        public static void TriggerActiveLayerChanged(WorldLayer layer)
        {
            OnActiveLayerChanged?.Invoke(layer);
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Clears all event subscriptions. Use when unloading or in cleanup.
        /// </summary>
        public static void ClearAllEvents()
        {
            // Layer Events
            OnLayerAdded = null;
            OnLayerRemoved = null;
            OnLayerCleared = null;

            // Grid Events
            OnTilePlaced = null;
            OnTileRemoved = null;
            OnGridCleared = null;
            OnGridInitialized = null;

            // Painting Events
            OnPaintingStarted = null;
            OnPaintingStopped = null;
            OnActiveLayerChanged = null;
        }

        #endregion
    }
}