using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyWorldGeneration.Data.SaveSystem
{
    [Serializable]
    public class WorldSaveData
    {
        public string worldName;
        public string version = "1.0";
        public long timestamp;
        public string author;

        public GridSettings gridSettings;
        public List<LayerSaveData> layers = new();

        public WorldSaveData()
        {
            timestamp = DateTime.Now.Ticks;
        }
    }

    [Serializable]
    public class GridSettings
    {
        public int width;
        public int height;
        public int maxLevels;
        public float tileSize;

        public GridSettings(int width, int height, int maxLevels, float tileSize)
        {
            this.width = width;
            this.height = height;
            this.maxLevels = maxLevels;
            this.tileSize = tileSize;
        }
    }

    [Serializable]
    public class LayerSaveData
    {
        public string layerName;
        public string layerGuid;
        public int layerLevel;
        public SerializableColor layerColor;
        public bool isEnabled;

        public List<TileSaveData> tiles = new();

        public LayerSaveData(string layerName, int layerLevel, Color color)
        {
            this.layerName = layerName;
            this.layerLevel = layerLevel;
            layerColor = new SerializableColor(color);
            isEnabled = true;
        }
    }

    [Serializable]
    public class TileSaveData
    {
        public int x;
        public int y;

        public TileSaveData(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    [Serializable]
    public class SerializableColor
    {
        public float r, g, b, a;

        public SerializableColor(Color color)
        {
            r = color.r;
            g = color.g;
            b = color.b;
            a = color.a;
        }

        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }
}