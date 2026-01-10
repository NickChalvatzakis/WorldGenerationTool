using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Events;
using UnityEngine;
using System.Collections.Generic;

namespace CozyWorldGeneration.Core.DualGrid
{
    public class WorldGrid : Grid3D<WorldTile>
    {
        public bool SuppressEvents { get; set; } = false;

        public WorldGrid(int width, int height, int maxLevels) : base(width, height, maxLevels)
        {
        }

        public void PlaceTile(int x, int y, WorldLayer layer)
        {
            if (!IsValidPosition(x, y) || layer == null) return;

            var tile = new WorldTile(x, y, layer);
            SetTile(x, y, layer.LayerLevel, tile);

            if (!SuppressEvents)
                ToolEvents.TriggerTileChanged(x, y);
        }

        public void RemoveTile(int x, int y, int level)
        {
            if (!HasTile(x, y, level)) return;
            SetTile(x, y, level, null);
            if (!SuppressEvents)
                ToolEvents.TriggerTileChanged(x, y);
        }

        public bool HasTileAt(int x, int y, int level)
        {
            return GetTile(x, y, level) != null;
        }

        /// <summary>
        /// Checks if a specific layer has a tile at this position.
        /// </summary>
        public bool HasTileForLayer(int x, int y, WorldLayer layer)
        {
            if (layer == null) return false;

            var tile = GetTile(x, y, layer.LayerLevel);
            return tile != null && tile.SourceLayer == layer;
        }

        public override void Clear()
        {
            base.Clear();
            if (!SuppressEvents)
                ToolEvents.TriggerGridCleared();
        }
    }
}