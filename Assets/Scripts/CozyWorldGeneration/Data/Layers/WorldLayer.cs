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
        [SerializeField] private Texture2D previewTexture;
        [SerializeField] private Color layerColor = Color.white;
        [SerializeField] private int defaultLayerHeight = 0;
        [SerializeField] private int layerLevel = 0;
        [SerializeField] private bool lockFromPaint = false;

        // Non-serialized UI state
        [NonSerialized] public bool foldoutState = false;

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
            get => previewTexture;
            set => previewTexture = value;
        }

        public Color LayerColor
        {
            get => layerColor;
            set => layerColor = value;
        }

        public int DefaultLayerHeight
        {
            get => defaultLayerHeight;
            set => defaultLayerHeight = value;
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
                return pixelColor.a > 0.5f; // Consider painted if alpha > 0.5
            }

            return false;
        }
    }
}