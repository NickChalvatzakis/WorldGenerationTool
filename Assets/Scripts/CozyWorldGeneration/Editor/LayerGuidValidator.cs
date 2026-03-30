using System.Collections.Generic;
using CozyWorldGeneration.Data.Layers;
using UnityEditor;
using UnityEngine;

namespace CozyWorldGeneration.Editor
{
    /// <summary>
    /// Detects and fixes duplicate internal GUIDs on WorldLayer and VisualLayer assets.
    ///
    /// WHY THIS EXISTS:
    /// Both layer types serialize a custom GUID field. Unity's built-in copy/paste
    /// duplicates the serialized bytes as-is, so the new asset inherits the same GUID.
    /// OnEnable() only regenerates if the field is empty, which never happens on a copy.
    /// This postprocessor runs after every import and regenerates the GUID on any
    /// newly-imported asset whose GUID already belongs to a different asset.
    /// </summary>
    public class LayerGuidValidator : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var anyFixed = false;

            foreach (var path in importedAssets)
            {
                var worldLayer = AssetDatabase.LoadAssetAtPath<WorldLayer>(path);
                if (worldLayer != null)
                {
                    if (EnsureUniqueGuid(worldLayer, path))
                        anyFixed = true;
                    continue;
                }

                var visualLayer = AssetDatabase.LoadAssetAtPath<VisualLayer>(path);
                if (visualLayer != null)
                {
                    if (EnsureUniqueGuid(visualLayer, path))
                        anyFixed = true;
                }
            }

            if (anyFixed)
                AssetDatabase.SaveAssets();
        }

        // ─── Per-type helpers ────────────────────────────────────────────────

        private static bool EnsureUniqueGuid(WorldLayer layer, string layerPath)
        {
            var allPaths = FindAllAssetPaths("t:WorldLayer");
            foreach (var otherPath in allPaths)
            {
                if (otherPath == layerPath) continue;
                var other = AssetDatabase.LoadAssetAtPath<WorldLayer>(otherPath);
                if (other != null && other.GUID == layer.GUID)
                {
                    Debug.LogWarning(
                        $"[LayerGuidValidator] Duplicate WorldLayer GUID found on '{layerPath}' " +
                        $"(conflicts with '{otherPath}'). Regenerating GUID.");
                    layer.RegenerateGuid();
                    EditorUtility.SetDirty(layer);
                    return true;
                }
            }
            return false;
        }

        private static bool EnsureUniqueGuid(VisualLayer layer, string layerPath)
        {
            var allPaths = FindAllAssetPaths("t:VisualLayer");
            foreach (var otherPath in allPaths)
            {
                if (otherPath == layerPath) continue;
                var other = AssetDatabase.LoadAssetAtPath<VisualLayer>(otherPath);
                if (other != null && other.GUID == layer.GUID)
                {
                    Debug.LogWarning(
                        $"[LayerGuidValidator] Duplicate VisualLayer GUID found on '{layerPath}' " +
                        $"(conflicts with '{otherPath}'). Regenerating GUID.");
                    layer.RegenerateGuid();
                    EditorUtility.SetDirty(layer);
                    return true;
                }
            }
            return false;
        }

        private static List<string> FindAllAssetPaths(string filter)
        {
            var guids = AssetDatabase.FindAssets(filter);
            var paths = new List<string>(guids.Length);
            foreach (var g in guids)
                paths.Add(AssetDatabase.GUIDToAssetPath(g));
            return paths;
        }

        // ─── Manual repair menu item ─────────────────────────────────────────

        [MenuItem("Tools/Cozy World Generation/Fix Duplicate Layer GUIDs", false, 50)]
        public static void FixAllDuplicateGuids()
        {
            var fixedCount = 0;

            // ── WorldLayers ──────────────────────────────────────────────────
            var wlPaths = FindAllAssetPaths("t:WorldLayer");
            // Build a seen-set; first asset encountered for each GUID wins.
            var seenWorldGuids = new Dictionary<string, string>(); // guid -> first path

            foreach (var path in wlPaths)
            {
                var layer = AssetDatabase.LoadAssetAtPath<WorldLayer>(path);
                if (layer == null) continue;

                if (seenWorldGuids.TryGetValue(layer.GUID, out var firstPath))
                {
                    Debug.LogWarning(
                        $"[LayerGuidValidator] Duplicate WorldLayer GUID: '{path}' vs '{firstPath}'. Regenerating '{path}'.");
                    layer.RegenerateGuid();
                    EditorUtility.SetDirty(layer);
                    fixedCount++;
                    // Register the new (now unique) GUID
                    seenWorldGuids[layer.GUID] = path;
                }
                else
                {
                    seenWorldGuids[layer.GUID] = path;
                }
            }

            // ── VisualLayers ─────────────────────────────────────────────────
            var vlPaths = FindAllAssetPaths("t:VisualLayer");
            var seenVisualGuids = new Dictionary<string, string>();

            foreach (var path in vlPaths)
            {
                var layer = AssetDatabase.LoadAssetAtPath<VisualLayer>(path);
                if (layer == null) continue;

                if (seenVisualGuids.TryGetValue(layer.GUID, out var firstPath))
                {
                    Debug.LogWarning(
                        $"[LayerGuidValidator] Duplicate VisualLayer GUID: '{path}' vs '{firstPath}'. Regenerating '{path}'.");
                    layer.RegenerateGuid();
                    EditorUtility.SetDirty(layer);
                    fixedCount++;
                    seenVisualGuids[layer.GUID] = path;
                }
                else
                {
                    seenVisualGuids[layer.GUID] = path;
                }
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[LayerGuidValidator] Fixed {fixedCount} duplicate GUID(s). Please re-save your world.");
            }
            else
            {
                Debug.Log("[LayerGuidValidator] No duplicate GUIDs found. All layers are clean.");
            }
        }
    }
}

