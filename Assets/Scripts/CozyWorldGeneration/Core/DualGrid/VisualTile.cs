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
            // Select a random tileset when layer is assigned
            if (layer != null) selectedTileset = layer.GetRandomTileset();
        }

        /// <summary>
        /// Updates the visual representation based on configuration index and tileset.
        /// </summary>
        public void UpdateVisual(Transform parent, float tileSize)
        {
            // Destroy old visual if it exists
            if (VisualInstance != null)
            {
                Object.DestroyImmediate(VisualInstance);
                VisualInstance = null;
            }

            // If no tileset or config is empty, don't render
            if (selectedTileset == null || ConfigurationIndex == 0)
                return;

            // Get the configuration from the tileset
            var config = selectedTileset.GetConfiguration(ConfigurationIndex);

            // If no mesh, don't render
            if (config.mesh == null)
                return;

            // Get or create layer-specific container
            var layerContainer = GetOrCreateLayerContainer(parent);

            // Create visual instance
            VisualInstance = new GameObject($"VisualTile_{GridPosition.x}_{GridPosition.y}");
            VisualInstance.transform.SetParent(layerContainer);

            // Calculate Y position: base level + visual offset
            var layerLevel = visualLayer?.AssignedWorldLayer?.LayerLevel ?? 0;
            var visualHeight = visualLayer?.VisualHeight ?? 0f;
            var levelHeight = 1f; // Base height per level (could be configurable)
            var finalY = layerLevel * levelHeight + visualHeight;

            // Position and rotate
            var worldPos = new Vector3(
                (GridPosition.x + 1.0f) * tileSize,
                finalY,
                (GridPosition.y + 1.0f) * tileSize
            );
            VisualInstance.transform.position = worldPos;
            VisualInstance.transform.rotation = config.GetRotation();

            // Add mesh components
            var meshFilter = VisualInstance.AddComponent<MeshFilter>();
            meshFilter.mesh = config.mesh;

            var meshRenderer = VisualInstance.AddComponent<MeshRenderer>();
            meshRenderer.material = config.material;
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