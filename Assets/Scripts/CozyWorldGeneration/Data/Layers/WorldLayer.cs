using System;
using CozyWorldGeneration.Core.Enums;
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
                if (previewTexture == null && textureData != null && textureData.Length > 0) RebuildTextureFromData();
                return previewTexture;
            }
        }

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
            else if (previewTexture != null && previewTexture.width > 0 && previewTexture.height > 0)
                // Has stored dimensions - create empty texture
                InitializePreviewTexture(previewTexture.width, previewTexture.height);
            else if (fallbackWidth > 0 && fallbackHeight > 0)
                // Use fallback dimensions from GridManager
                InitializePreviewTexture(fallbackWidth, fallbackHeight);
            else
                Debug.LogWarning($"[WorldLayer] Cannot rebuild texture for '{layerName}' - no dimensions available");
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
            if (string.IsNullOrEmpty(guid)) guid = Guid.NewGuid().ToString();
            if (textureData != null && textureData.Length > 0) previewTexture.LoadImage(textureData);
        }

        /// <summary>
        /// Initializes the preview texture based on grid size.
        /// </summary>
        public void InitializePreviewTexture(int width, int height)
        {
            previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            previewTexture.filterMode = FilterMode.Point;
            ClearPreviewTexture();
        }

        /// <summary>
        /// Clears the preview texture to transparent.
        /// </summary>
        public void ClearPreviewTexture()
        {
            if (previewTexture != null)
            {
                var clearColors = new Color[previewTexture.width * previewTexture.height];
                for (var i = 0; i < clearColors.Length; i++) clearColors[i] = Color.clear;
                previewTexture.SetPixels(clearColors);
                previewTexture.Apply();

                // Raise event through centralized system
                ToolEvents.RaiseLayerCleared(this);

#if UNITY_EDITOR
                Debug.Log($"[WorldLayer] Cleared preview texture for {LayerName}");
#endif
            }
        }

        /// <summary>
        /// Paints a pixel at the specified grid position.
        /// </summary>
        public void PaintPixel(int x, int y, bool paint = true)
        {
            if (previewTexture != null && x >= 0 && x < previewTexture.width && y >= 0 && y < previewTexture.height)
            {
                previewTexture.SetPixel(x, y, paint ? layerColor : Color.clear);
                previewTexture.Apply();
                textureData = previewTexture.EncodeToPNG();
            }
        }

        /// <summary>
        /// Checks if a pixel is painted at the specified position.
        /// </summary>
        public bool IsPixelPainted(int x, int y)
        {
            if (previewTexture != null && x >= 0 && x < previewTexture.width && y >= 0 && y < previewTexture.height)
            {
                var pixelColor = previewTexture.GetPixel(x, y);
                return pixelColor.a > 0.5f;
            }

            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (textureData != null && textureData.Length > 0) RebuildTextureFromData();
        }
#endif
    }
}