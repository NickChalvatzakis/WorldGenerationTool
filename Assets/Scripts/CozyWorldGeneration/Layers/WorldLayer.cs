using UnityEngine;

namespace CozyWorldGeneration.Layers
{
    [CreateAssetMenu(menuName = "CozyWorldGeneration/Layers/WorldLayer")]
    public class WorldLayer : ScriptableObject
    {
        [SerializeField] private string guid = System.Guid.NewGuid().ToString();
        [SerializeField] private string layerName = "New Layer";
        [SerializeField] private bool isEnabled = true;
        [SerializeField] private Texture2D previewTexture;
        [SerializeField] private Color layerColor = Color.white;
        [SerializeField] private int defaultLayerHeight = 0;
        [SerializeField] private TileType tileType = TileType.Grass;
        [SerializeField] private bool lockFromPaint = false;

        public bool foldoutState = false;
        
        public string GUID { get => guid; private set => guid = value; }
        public string LayerName { get => layerName;  set => layerName = value; }
        public bool IsEnabled { get => isEnabled;  set => isEnabled = value; }
        public TileType TileType { get => tileType;  set => tileType = value; }
        public Texture2D PreviewTexture { get => previewTexture;  set => previewTexture = value; }
        public Color LayerColor { get => layerColor; set => layerColor = value; }
        public int DefaultLayerHeight { get => defaultLayerHeight;  set => defaultLayerHeight = value; }
        public bool LockFromPaint { get => lockFromPaint;  set => lockFromPaint = value; }

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(guid))
            {
                guid = System.Guid.NewGuid().ToString();
            }
        }
        
        public void InitializePreviewTexture(int width, int height)
        {
            if (previewTexture == null || previewTexture.width != width || previewTexture.height != height)
            {
                previewTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                previewTexture.filterMode = FilterMode.Point;
                ClearPreviewTexture();
            }
        }
        
        public void ClearPreviewTexture()
        {
            if (previewTexture != null)
            {
                Color[] clearColors = new Color[previewTexture.width * previewTexture.height];
                for (int i = 0; i < clearColors.Length; i++)
                {
                    clearColors[i] = Color.clear;
                }
                previewTexture.SetPixels(clearColors);
                previewTexture.Apply();
            }
        }
        
        public void PaintPixel(int x, int y, bool paint = true)
        {
            if (previewTexture != null && x >= 0 && x < previewTexture.width && y >= 0 && y < previewTexture.height)
            {
                previewTexture.SetPixel(x, y, paint ? layerColor : Color.clear);
                previewTexture.Apply();
            }
        }
        
        public bool IsPixelPainted(int x, int y)
        {
            if (previewTexture != null && x >= 0 && x < previewTexture.width && y >= 0 && y < previewTexture.height)
            {
                Color pixelColor = previewTexture.GetPixel(x, y);
                return pixelColor.a > 0.5f; // Consider painted if alpha > 0.5
            }
            return false;
        }


    }
}