using CozyWorldGeneration.Data.Layers;
using CozyWorldGeneration.Data.Tilesets;
using UnityEngine;

namespace CozyWorldGeneration.Core.DualGrid
{
    public class VisualTile
    {
        public Vector2Int GridPosition { get; private set; }
        public int ConfigurationIndex { get; set; }
        public GameObject VisualInstance { get; set; }
        public float FillLevel { get; set; }

        private VisualLayer visualLayer;
        private Tileset selectedTileset;

        public VisualTile(int x, int y)
        {
            GridPosition = new Vector2Int(x, y);
            ConfigurationIndex = 0;
            FillLevel = 1.0f;
        }

        /// <summary>
        /// Sets the visual layer this tile should use for rendering.
        /// </summary>
        public void SetVisualLayer(VisualLayer layer)
        {
            visualLayer = layer;
            if (layer != null) selectedTileset = layer.GetRandomTileset();
        }

        /// <summary>
        /// Updates the visual representation based on configuration index and tileset.
        /// </summary>
        public void UpdateVisual(
            Transform parent,
            float tileHeightOffset,
            float tileSize = 1.0f,
            Vector2 flowDirection = default,
            int renderLevel = -1)
        {
            if (VisualInstance != null)
            {
                Object.DestroyImmediate(VisualInstance);
                VisualInstance = null;
            }

            if (selectedTileset == null || ConfigurationIndex == 0)
                return;

            var config = selectedTileset.GetConfiguration(ConfigurationIndex);
            if (config.mesh == null)
                return;

            var layerContainer = GetOrCreateLayerContainer(parent);

            VisualInstance = new GameObject($"VisualTile_{GridPosition.x}_{GridPosition.y}");
            VisualInstance.transform.SetParent(layerContainer);

            // Use explicit renderLevel when provided (fluid), otherwise fallback to layer mapping (terrain)
            var fallbackLayerLevel = visualLayer?.AssignedWorldLayer?.LayerLevel ?? 0;
            var effectiveLevel = renderLevel >= 0 ? renderLevel : fallbackLayerLevel;
            var visualHeight = visualLayer?.VisualHeight ?? 0f;
            var levelHeight = 1f;
            var finalY = effectiveLevel * levelHeight + visualHeight;

            var worldPos = new Vector3(
                (GridPosition.x + 1.0f) * tileHeightOffset,
                finalY,
                (GridPosition.y + 1.0f) * tileHeightOffset
            );

            VisualInstance.transform.position = worldPos;
            VisualInstance.transform.rotation = config.GetRotation();
            VisualInstance.transform.localScale = new Vector3(1.0f, tileSize, 1.0f);

            var meshFilter = VisualInstance.AddComponent<MeshFilter>();
            meshFilter.mesh = config.mesh;

            var meshRenderer = VisualInstance.AddComponent<MeshRenderer>();
            meshRenderer.material = config.material;
            meshRenderer.sharedMaterial.SetVector("_FlowDirection", flowDirection);
        }

        /// <summary>
        /// Gets or creates a container for this visual layer.
        /// </summary>
        private Transform GetOrCreateLayerContainer(Transform parent)
        {
            if (visualLayer == null || parent == null)
                return parent;

            var containerName = $"Layer_{visualLayer.LayerName}";
            var existing = parent.Find(containerName);

            if (existing != null)
                return existing;

            var container = new GameObject(containerName);
            container.transform.SetParent(parent);
            container.transform.localPosition = Vector3.zero;
            return container.transform;
        }

        /// <summary>
        /// Destroys the visual GameObject if it exists.
        /// </summary>
        public void DestroyVisual()
        {
            if (VisualInstance != null)
            {
                Object.DestroyImmediate(VisualInstance);
                VisualInstance = null;
            }
        }
    }
}