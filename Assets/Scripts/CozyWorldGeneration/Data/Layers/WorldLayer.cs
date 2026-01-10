using System;
using CozyWorldGeneration.Events;
using UnityEngine;

namespace CozyWorldGeneration.Data.Layers
{
    /// <summary>
    /// ScriptableObject that represents a single layer in the grid system.
    /// Can be painted on to define where tiles exist.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWorldLayer", menuName = "Cozy World Generation/World Layer")]
    public class WorldLayer : ScriptableObject
    {
        [SerializeField] private string guid = Guid.NewGuid().ToString();
        [SerializeField] private string layerName = "New Layer";
        [SerializeField] private bool isEnabled = true;
        [NonSerialized] private Texture2D previewTexture;
        [SerializeField] private Color layerColor = Color.white;
        [SerializeField] private int layerLevel = 0;
        [SerializeField] private bool lockFromPaint = false;

        // Non-serialized UI state
        [NonSerialized] public bool foldoutState = false;

        [SerializeField] [HideInInspector] private byte[] textureData;

        public string GUID
        {
            get => guid;
            private set => guid = value;
        }

        public string LayerName
        {
            get => layerName;
            set => layerName = value;
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => isEnabled = value;
        }

        public Texture2D PreviewTexture
        {
            get
            {
                // Lazy initialization - rebuild from serialized data if needed
                if (previewTexture == null && textureData != null && textureData.Length > 0)
                    RebuildTextureFromData();
                return previewTexture;
            }
        }

        public Color LayerColor
        {
            get => layerColor;
            set => layerColor = value;
        }

        public int LayerLevel
        {
            get => layerLevel;
            set => layerLevel = value;
        }

        public bool LockFromPaint
        {
            get => lockFromPaint;
            set => lockFromPaint = value;
        }

        private void OnEnable()
        {
            // Ensure GUID exists
            if (string.IsNullOrEmpty(guid))
                guid = Guid.NewGuid().ToString();

            // Rebuild texture from serialized data if available
            if (textureData != null && textureData.Length > 0)
                RebuildTextureFromData();
        }

        /// <summary>
        /// Rebuilds the preview texture from serialized PNG data
        /// </summary>
        public void RebuildTextureFromData()
        {
            if (textureData == null || textureData.Length == 0) return;

            previewTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            previewTexture.filterMode = FilterMode.Point;

            if (previewTexture.LoadImage(textureData))
                Debug.Log(
                    $"[WorldLayer] Rebuilt texture for {layerName} ({previewTexture.width}x{previewTexture.height})");
            else
                Debug.LogWarning($"[WorldLayer] Failed to rebuild texture for {layerName}");
        }

        public void ForceRebuildTexture(int fallbackWidth = 0, int fallbackHeight = 0)
        {
            previewTexture = null;

            if (textureData != null && textureData.Length > 0)
                // Has serialized data - rebuild from it
                RebuildTextureFromData();
            else if (fallbackWidth > 0 && fallbackHeight > 0)
                // Use fallback dimensions from GridManager
                InitializePreviewTexture(fallbackWidth, fallbackHeight);
            else
                Debug.LogWarning($"[WorldLayer] Cannot rebuild texture for '{layerName}' - no dimensions available");
        }

        public void InitializePreviewTexture(int width, int height)
        {
            previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            previewTexture.filterMode = FilterMode.Point;
            ClearPreviewTexture();
        }

        public void ClearPreviewTexture()
        {
            if (previewTexture != null)
            {
                var clearColors = new Color[previewTexture.width * previewTexture.height];
                for (var i = 0; i < clearColors.Length; i++)
                    clearColors[i] = Color.clear;

                previewTexture.SetPixels(clearColors);
                previewTexture.Apply();

                // Serialize after clearing
                SerializeTextureData();

                // Trigger event through centralized system
                ToolEvents.TriggerLayerCleared(this);

#if UNITY_EDITOR
                Debug.Log($"[WorldLayer] Cleared preview texture for {LayerName}");
#endif
            }
        }

        public void PaintPixel(int x, int y, bool paint = true)
        {
            if (previewTexture != null &&
                x >= 0 && x < previewTexture.width &&
                y >= 0 && y < previewTexture.height)
            {
                previewTexture.SetPixel(x, y, paint ? layerColor : Color.clear);
                previewTexture.Apply();
            }
        }

        public bool IsPixelPainted(int x, int y)
        {
            if (previewTexture != null &&
                x >= 0 && x < previewTexture.width &&
                y >= 0 && y < previewTexture.height)
            {
                var pixelColor = previewTexture.GetPixel(x, y);
                return pixelColor.a > 0.5f;
            }

            return false;
        }

        /// <summary>
        /// Serializes the current texture to PNG data for persistence.
        /// Should be called:
        /// - At the end of a paint stroke (GridPainterTool.EndPaintStroke)
        /// - When saving the world (WorldSaveManager.SaveWorld)
        /// - When clearing the layer
        /// </summary>
        public void SerializeTextureData()
        {
            if (previewTexture != null)
            {
                textureData = previewTexture.EncodeToPNG();
#if UNITY_EDITOR
                Debug.Log($"[WorldLayer] Serialized texture data for {layerName} ({textureData.Length} bytes)");
#endif
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (textureData != null && textureData.Length > 0)
                RebuildTextureFromData();
        }
#endif
    }
}