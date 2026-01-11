using System;
using CozyWorldGeneration.Core.DualGrid;
using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Core.Fluids;
using CozyWorldGeneration.Data.Layers;
using UnityEngine;

namespace CozyWorldGeneration.Core.Events
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

        #region Fluid Events

        /// <summary>
        /// Fired when fluid is placed. Passes the WorldTile that has the fluid.
        /// </summary>
        public static event Action<WorldTile> OnFluidPlaced;
        
        /// <summary>
        /// Fired when fluid is removed from a position.
        /// </summary>
        public static event Action<Vector3Int> OnFluidRemoved;
        
        /// <summary>
        /// Fired when a fluid body settles (stops flowing).
        /// </summary>
        public static event Action<FluidBody> OnFluidBodySettled;
        
        /// <summary>
        /// Fired when a settled fluid body becomes unsettled.
        /// </summary>
        public static event Action<FluidBody> OnFluidBodyUnsettled;

        /// <summary>
        /// Fired when a fluid body spreads to new tiles.
        /// </summary>
        public static event Action<FluidBody> OnFluidBodySpread;

        /// <summary>
        /// Fired each time the fluid simulation completes a tick.
        /// </summary>
        public static event Action OnFluidSimulationTick;

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

        // Fluid Events
        public static void TriggerFluidPlaced(WorldTile tile)
        {
            OnFluidPlaced?.Invoke(tile);
        }

        public static void TriggerFluidRemoved(Vector3Int pos)
        {
            OnFluidRemoved?.Invoke(pos);
        }

        public static void TriggerFluidBodySpread(FluidBody body)
        {
            OnFluidBodySpread?.Invoke(body);
        }

        public static void TriggerFluidBodySettled(FluidBody body)
        {
            OnFluidBodySettled?.Invoke(body);
        }

        public static void TriggerFluidBodyUnsettled(FluidBody body)
        {
            OnFluidBodyUnsettled?.Invoke(body);
        }

        public static void TriggerFluidSimulationTick()
        {
            OnFluidSimulationTick?.Invoke();
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
            OnTileChanged = null;
            OnGridCleared = null;
            OnGridInitialized = null;

            // Fluid Events
            OnFluidPlaced = null;
            OnFluidRemoved = null;
            OnFluidBodySettled = null;
            OnFluidBodyUnsettled = null;
            OnFluidBodySpread = null;
            OnFluidSimulationTick = null;

            // Painting Events
            OnPaintingStarted = null;
            OnPaintingStopped = null;
            OnActiveLayerChanged = null;
        }

        #endregion
    }
}
