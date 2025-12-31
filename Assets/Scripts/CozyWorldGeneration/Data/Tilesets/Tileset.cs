using CozyWorldGeneration.Core.Enums;
using UnityEngine;

namespace CozyWorldGeneration.Data.Tilesets
{
    /// <summary>
    /// Defines a tileset with meshes and materials for the dual grid system.
    /// User provides base meshes with their TileType and rotation, system generates all 16 configs.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTileset", menuName = "Cozy World Generation/Tileset")]
    public class Tileset : ScriptableObject
    {
        [Header("Base Mesh Configurations")] [Tooltip("Define your base meshes with their type and current rotation")]
        public BaseMeshConfiguration cornerConfig;

        public BaseMeshConfiguration edgeConfig;
        public BaseMeshConfiguration interiorCornerConfig;
        public BaseMeshConfiguration diagonalConfig;
        public BaseMeshConfiguration fillConfig;

        [Header("Material")] [Tooltip("Material applied to all meshes in this tileset")]
        public Material material;

        [Header("Generated Configurations")] [Tooltip("Auto-generated mapping of 16 configs")]
        public TileConfiguration[] configurations = new TileConfiguration[16];

        private void OnValidate()
        {
            GenerateConfigurations();
        }

        /// <summary>
        /// Generates all 16 configurations based on the base mesh configs provided.
        /// </summary>
        [ContextMenu("Generate Configurations")]
        public void GenerateConfigurations()
        {
            configurations = new TileConfiguration[16];

            // Config 0: Always empty
            configurations[0] = new TileConfiguration
            {
                tileType = TileType.Empty,
                mesh = null,
                rotationY = 0,
                material = material
            };

            // Generate Corner configs (1, 2, 4, 8)
            if (cornerConfig.mesh != null) GenerateRotatedConfigs(cornerConfig, GetCornerConfigs());

            // Generate Edge configs (3, 6, 9, 12)
            if (edgeConfig.mesh != null) GenerateRotatedConfigs(edgeConfig, GetEdgeConfigs());

            // Generate Diagonal configs (5, 10)
            if (diagonalConfig.mesh != null) GenerateRotatedConfigs(diagonalConfig, GetDiagonalConfigs());

            // Generate Interior Corner configs (7, 11, 13, 14)
            if (interiorCornerConfig.mesh != null)
                GenerateRotatedConfigs(interiorCornerConfig, GetInteriorCornerConfigs());

            // Generate Fill config (15)
            if (fillConfig.mesh != null)
                configurations[15] = new TileConfiguration
                {
                    tileType = TileType.Fill,
                    mesh = fillConfig.mesh,
                    rotationY = fillConfig.baseRotation,
                    material = material
                };

            Debug.Log($"Generated {configurations.Length} configurations for tileset: {name}");
        }

        /// <summary>
        /// Generates rotated versions of a base mesh config for multiple configuration indices.
        /// </summary>
        private void GenerateRotatedConfigs(BaseMeshConfiguration baseConfig, int[] configIndices)
        {
            // Find which config index the base rotation corresponds to
            var baseIndex = FindBaseConfigIndex(baseConfig.tileType, baseConfig.baseRotation);

            if (baseIndex == -1)
            {
                Debug.LogWarning(
                    $"Could not determine base config for {baseConfig.tileType} at rotation {baseConfig.baseRotation}");
                return;
            }

            // Find where the base is in the array
            var basePosition = System.Array.IndexOf(configIndices, baseIndex);

            // Generate all rotations relative to the base
            for (var i = 0; i < configIndices.Length; i++)
            {
                var rotationSteps = (i - basePosition + configIndices.Length) % configIndices.Length;
                var rotation = rotationSteps * 90f;


                configurations[configIndices[i]] = new TileConfiguration
                {
                    tileType = baseConfig.tileType,
                    mesh = baseConfig.mesh,
                    rotationY = rotation,
                    material = material
                };
            }
        }

        /// <summary>
        /// Determines which config index a mesh with given type and rotation belongs to.
        /// Bit pattern: bit0=bottomLeft, bit1=bottomRight, bit2=topLeft, bit3=topRight
        /// </summary>
        private int FindBaseConfigIndex(TileType type, float rotation)
        {
            // Normalize rotation to 0-360
            rotation = rotation % 360f;
            if (rotation < 0) rotation += 360f;

            switch (type)
            {
                case TileType.Corner:
                    if (Mathf.Approximately(rotation, 0f)) return 1; // Bottom-left
                    if (Mathf.Approximately(rotation, 90f)) return 4; // Top-left (rotate 90° CCW)
                    if (Mathf.Approximately(rotation, 180f)) return 8; // Top-right (rotate 180°)
                    if (Mathf.Approximately(rotation, 270f)) return 2; // Bottom-right (rotate 270° CCW)
                    break;

                case TileType.Edge:
                    if (Mathf.Approximately(rotation, 0f)) return 3; // Bottom
                    if (Mathf.Approximately(rotation, 90f)) return 5; // Left
                    if (Mathf.Approximately(rotation, 180f)) return 12; // Top
                    if (Mathf.Approximately(rotation, 270f)) return 10; // Right
                    break;

                case TileType.Diagonal:
                    if (Mathf.Approximately(rotation, 0f)) return 9; // BL + TR filled
                    if (Mathf.Approximately(rotation, 90f)) return 6; // BR + TL filled
                    break;

                case TileType.InnerCorner:
                    if (Mathf.Approximately(rotation, 0f)) return 11; // Missing top-left
                    if (Mathf.Approximately(rotation, 90f)) return 7; // Missing top-right
                    if (Mathf.Approximately(rotation, 180f)) return 13; // Missing bottom-right
                    if (Mathf.Approximately(rotation, 270f)) return 14; // Missing bottom-left
                    break;
            }

            return -1;
        }

        private int[] GetCornerConfigs()
        {
            return new int[] { 1, 4, 8, 2 };
            // 0°, 90°, 180°, 270°
        }

        private int[] GetEdgeConfigs()
        {
            return new int[] { 3, 5, 12, 10 }; // bottom, left, top, right
        }

        private int[] GetDiagonalConfigs()
        {
            return new int[] { 9, 6 }; // BL+TR at 0°, BR+TL at 90°
        }

        private int[] GetInteriorCornerConfigs()
        {
            return new int[] { 11, 7, 13, 14 }; // TL, TR, BR, BL missing (0°, 90°, 180°, 270°)
        }

        /// <summary>
        /// Gets the configuration for a specific index.
        /// </summary>
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

    /// <summary>
    /// User-defined base mesh with its type and rotation.
    /// System uses this to generate all rotated variants.
    /// </summary>
    [System.Serializable]
    public class BaseMeshConfiguration
    {
        public TileType tileType;
        public Mesh mesh;

        [Tooltip("The rotation of THIS specific mesh (0, 90, 180, or 270)")]
        public float baseRotation = 0f;
    }

    /// <summary>
    /// Defines a single configuration: type, mesh, rotation, and material.
    /// </summary>
    [System.Serializable]
    public struct TileConfiguration
    {
        public TileType tileType;
        public Mesh mesh;
        public float rotationY;
        public Material material;

        public Quaternion GetRotation()
        {
            return Quaternion.Euler(0, rotationY, 0);
        }
    }
}