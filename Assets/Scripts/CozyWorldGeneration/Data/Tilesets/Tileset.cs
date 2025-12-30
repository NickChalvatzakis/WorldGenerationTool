using UnityEngine;

namespace CozyWorldGeneration.Data.Tilesets
{
    [CreateAssetMenu(fileName = "NewTileset", menuName = "Cozy World Generation/Tileset Definition")]
    public class Tileset : ScriptableObject
    {
        [Header("Base Meshes")] [Tooltip("Single filled corner (rotated for configs 1,2,4,8)")]
        public Mesh cornerMesh;

        [Tooltip("Two adjacent filled tiles - edge (rotated for configs 3,6,9,12)")]
        public Mesh edgeMesh;

        [Tooltip("Three filled, one empty - interior corner (rotated for configs 7,11,13,14)")]
        public Mesh interiorCornerMesh;

        [Tooltip("Two diagonal filled corners (rotated for configs 5,10)")]
        public Mesh doubleDiagonalMesh;

        [Tooltip("All four corners filled (config 15)")]
        public Mesh fillMesh;

        [Header("Material")] [Tooltip("Material applied to all meshes in this tileset")]
        public Material material;

        [Header("Configuration Mapping")] [Tooltip("Auto-generated mapping of 16 configs to mesh+rotation")]
        public TileConfiguration[] configurations = new TileConfiguration[16];

        private void OnValidate()
        {
            // Auto-generate the configuration mapping
            GenerateConfigurations();
        }

        [ContextMenu("Generate Configurations")]
        public void GenerateConfigurations()
        {
            configurations = new TileConfiguration[16];

            // Config 0: Empty (no mesh needed)
            configurations[0] = new TileConfiguration { mesh = null, rotationY = 0, material = material };

            // Configs 1,2,4,8: Single corner (4 rotations)
            configurations[1] = new TileConfiguration
                { mesh = cornerMesh, rotationY = 0, material = material }; // Bottom-Left
            configurations[2] = new TileConfiguration
                { mesh = cornerMesh, rotationY = 270, material = material }; // Bottom-Right
            configurations[4] = new TileConfiguration
                { mesh = cornerMesh, rotationY = 90, material = material }; // Top-Left
            configurations[8] = new TileConfiguration
                { mesh = cornerMesh, rotationY = 180, material = material }; // Top-Right

            // Configs 3,6,9,12: Edge (4 rotations)
            configurations[3] = new TileConfiguration
                { mesh = edgeMesh, rotationY = 0, material = material }; // Bottom edge
            configurations[6] = new TileConfiguration
                { mesh = edgeMesh, rotationY = 90, material = material }; // Left edge
            configurations[9] = new TileConfiguration
                { mesh = edgeMesh, rotationY = 270, material = material }; // Top edge
            configurations[12] = new TileConfiguration
                { mesh = edgeMesh, rotationY = 180, material = material }; // Right edge

            // Configs 5,10: Double diagonal (2 rotations)
            configurations[5] = new TileConfiguration
                { mesh = doubleDiagonalMesh, rotationY = 0, material = material }; // Diagonal \
            configurations[10] = new TileConfiguration
                { mesh = doubleDiagonalMesh, rotationY = 90, material = material }; // Diagonal /

            // Configs 7,11,13,14: Interior corner (4 rotations)
            configurations[7] = new TileConfiguration
                { mesh = interiorCornerMesh, rotationY = 180, material = material }; // Missing top-right
            configurations[11] = new TileConfiguration
                { mesh = interiorCornerMesh, rotationY = 270, material = material }; // Missing top-left
            configurations[13] = new TileConfiguration
                { mesh = interiorCornerMesh, rotationY = 0, material = material }; // Missing bottom-right
            configurations[14] = new TileConfiguration
                { mesh = interiorCornerMesh, rotationY = 90, material = material }; // Missing bottom-left

            // Config 15: Fill (all filled)
            configurations[15] = new TileConfiguration { mesh = fillMesh, rotationY = 0, material = material };

            Debug.Log($"Generated configurations for tileset: {name}");
        }

        public TileConfiguration GetConfiguration(int configIndex)
        {
            if (configIndex < 0 || configIndex >= 16)
            {
                Debug.LogWarning($"Invalid config index: {configIndex}");
                return new TileConfiguration();
            }

            return configurations[configIndex];
        }
    }

    [System.Serializable]
    public struct TileConfiguration
    {
        public Mesh mesh;
        public float rotationY;
        public Material material;

        public Quaternion GetRotation()
        {
            return Quaternion.Euler(0, rotationY, 0);
        }
    }
}