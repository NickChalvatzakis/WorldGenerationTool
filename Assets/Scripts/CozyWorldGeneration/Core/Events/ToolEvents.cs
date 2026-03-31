using System;
using CozyWorldGeneration.Core.DualGrid;
using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Core.Fluids;
using CozyWorldGeneration.Data.Layers;
using UnityEngine;

namespace CozyWorldGeneration.Core.Events
{
    /// <summary>
    /// Static event hub for the tool. Subscribe here; GridManager and other listeners react to changes.
    /// </summary>
    public static class ToolEvents
    {
        #region Layer Events

        public static event Action<WorldLayer> OnLayerAdded;
        public static event Action<WorldLayer> OnLayerRemoved;
        public static event Action<WorldLayer> OnLayerCleared;

        #endregion

        #region Grid Events

        public static event Action<int, int, TileType, WorldLayer> OnTilePlaced;
        public static event Action<int, int> OnTileRemoved;
        public static event Action<int, int> OnTileChanged;
        public static event Action OnGridCleared;
        public static event Action<int, int> OnGridInitialized;

        #endregion

        #region Fluid Events

        public static event Action<WorldTile> OnFluidPlaced;
        public static event Action<Vector3Int> OnFluidRemoved;
        public static event Action<FluidBody> OnFluidBodySettled;
        public static event Action<FluidBody> OnFluidBodyUnsettled;
        public static event Action<FluidBody> OnFluidBodySpread;
        public static event Action OnFluidSimulationTick;

        #endregion

        #region Painting Events

        public static event Action<WorldLayer> OnPaintingStarted;
        public static event Action OnPaintingStopped;
        public static event Action<WorldLayer> OnActiveLayerChanged;

        #endregion

        #region Event Invocation Methods

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
            OnLayerAdded = null;
            OnLayerRemoved = null;
            OnLayerCleared = null;

            OnTilePlaced = null;
            OnTileRemoved = null;
            OnTileChanged = null;
            OnGridCleared = null;
            OnGridInitialized = null;

            OnFluidPlaced = null;
            OnFluidRemoved = null;
            OnFluidBodySettled = null;
            OnFluidBodyUnsettled = null;
            OnFluidBodySpread = null;
            OnFluidSimulationTick = null;

            OnPaintingStarted = null;
            OnPaintingStopped = null;
            OnActiveLayerChanged = null;
        }

        #endregion
    }
}
