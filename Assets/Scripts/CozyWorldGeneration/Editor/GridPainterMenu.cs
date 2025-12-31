using CozyWorldGeneration.Core;
using CozyWorldGeneration.Data.Layers;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace CozyWorldGeneration.Editor
{
    /// <summary>
    /// Menu items for the Grid Painter system.
    /// </summary>
    public static class GridPainterMenu
    {
        private const string MENU_PATH = "Tools/Cozy World Generaion/";
        private const string OVERLAY_PREF_KEY = "CozyWorld_PainterOverlayEnabled";

        [MenuItem(MENU_PATH + "Toggle Painter Overlay", false, 1)]
        public static void TogglePainterOverlay()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                Debug.LogWarning("No active Scene View found");
                return;
            }

            // Use TryGetOverlay with the overlay ID (which is the display name)
            if (sceneView.TryGetOverlay("Grid Painter", out var overlay))
            {
                overlay.displayed = !overlay.displayed;
                Debug.Log($"Grid Painter Overlay: {(overlay.displayed ? "Enabled" : "Disabled")}");
                sceneView.Repaint();
            }
            else
            {
                Debug.LogWarning(
                    "Grid Painter Overlay not found. It may need to be initialized first - open Scene View overlay menu.");
            }
        }

        [MenuItem(MENU_PATH + "Toggle Painter Overlay", true)]
        public static bool TogglePainterOverlayValidate()
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null && sceneView.TryGetOverlay("Grid Painter", out var overlay))
                Menu.SetChecked(MENU_PATH + "Toggle Painter Overlay", overlay.displayed);
            return sceneView != null;
        }

        [MenuItem(MENU_PATH + "Create Grid Manager", false, 10)]
        public static void CreateGridManager()
        {
            var go = new GameObject("GridManager");
            go.AddComponent<GridManager>();
            Selection.activeGameObject = go;

            Debug.Log("Created new GridManager");
        }

        [MenuItem(MENU_PATH + "Create World Layer", false, 11)]
        public static void CreateWorldLayer()
        {
            var layer = ScriptableObject.CreateInstance<WorldLayer>();
            layer.LayerName = "New World Layer";

            var path = EditorUtility.SaveFilePanelInProject(
                "Create World Layer",
                "NewWorldLayer",
                "asset",
                "Create a new World Layer asset"
            );

            if (!string.IsNullOrEmpty(path))
            {
                AssetDatabase.CreateAsset(layer, path);
                AssetDatabase.SaveAssets();
                Selection.activeObject = layer;

                Debug.Log($"Created World Layer: {path}");
            }
        }

        [MenuItem(MENU_PATH + "Focus on Grid Manager", false, 20)]
        public static void FocusOnGridManager()
        {
            var gridManager = GameObject.FindAnyObjectByType<GridManager>();

            if (gridManager != null)
            {
                Selection.activeGameObject = gridManager.gameObject;
                SceneView.lastActiveSceneView?.FrameSelected();
                Debug.Log("Focused on GridManager");
            }
            else
            {
                Debug.LogWarning("No GridManager found in scene");
            }
        }

        [MenuItem(MENU_PATH + "Focus on Grid Manager", true)]
        public static bool FocusOnGridManagerValidate()
        {
            return GameObject.FindAnyObjectByType<GridManager>() != null;
        }

        [MenuItem(MENU_PATH + "Clear All Layers", false, 30)]
        public static void ClearAllLayers()
        {
            var gridManager = GameObject.FindAnyObjectByType<GridManager>();

            if (gridManager == null)
            {
                EditorUtility.DisplayDialog("No GridManager",
                    "No GridManager found in scene", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog("Clear All Layers",
                    "This will clear ALL painted data from ALL layers. Continue?",
                    "Yes", "No"))
                return;

            var clearedCount = 0;

            // Clear world layers
            if (gridManager.WorldLayerCollection != null)
                foreach (var layer in gridManager.WorldLayerCollection.Layers)
                    if (layer != null)
                    {
                        layer.ClearPreviewTexture();
                        EditorUtility.SetDirty(layer);
                        clearedCount++;
                    }

            // Clear visual layers
            if (gridManager.VisualLayerCollection != null)
                foreach (var layer in gridManager.VisualLayerCollection.Layers)
                    if (layer != null)
                    {
                        EditorUtility.SetDirty(layer);
                        clearedCount++;
                    }

            AssetDatabase.SaveAssets();
            SceneView.RepaintAll();

            Debug.Log($"Cleared {clearedCount} layers");
        }

        [MenuItem(MENU_PATH + "Clear All Layers", true)]
        public static bool ClearAllLayersValidate()
        {
            return GameObject.FindAnyObjectByType<GridManager>() != null;
        }
    }
}