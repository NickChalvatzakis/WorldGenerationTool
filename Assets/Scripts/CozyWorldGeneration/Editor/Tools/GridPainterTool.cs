using System.Collections.Generic;
using System.Linq;
using CozyWorldGeneration.Core;
using CozyWorldGeneration.Core.Enums;
using CozyWorldGeneration.Data.Fluids;
using CozyWorldGeneration.Data.Layers;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace CozyWorldGeneration.Editor
{
    [EditorTool("Grid Painter Tool")]
    public class GridPainterTool : EditorTool
    {
        private GridManager gridManager;
        private WorldLayer selectedLayer;
        private FluidType selectedFluidType;
        private PaintMode paintMode = PaintMode.Terrain;
        private int fluidAmount = 7;
        private bool placeAsSource = false;

        private bool isPainting = false;
        private bool isErasing = false;

        private int undoGroup;
        private HashSet<WorldLayer> modifiedLayers = new();
        private HashSet<WorldLayer> modifiedLayersInCurrentStroke = new();

        private int brushSize = 1;

        public int BrushSize
        {
            get => brushSize;
            set => brushSize = Mathf.Max(1, value);
        }

        public override void OnActivated()
        {
            base.OnActivated();
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        public override void OnWillBeDeactivated()
        {
            base.OnWillBeDeactivated();
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            SerializeAllModifiedLayers();
        }

        /// <summary>
        /// Rebuilds layer textures and the WorldGrid from serialized data after undo/redo.
        /// </summary>
        private void OnUndoRedoPerformed()
        {
            if (gridManager == null)
                return;

            if (gridManager.WorldLayerCollection?.Layers != null)
                foreach (var layer in gridManager.WorldLayerCollection.Layers)
                    layer?.ForceRebuildTexture(gridManager.Width, gridManager.Height);

            if (gridManager.WorldGrid != null)
                RebuildWorldGridFromLayers();

            SceneView.RepaintAll();
        }

        private void RebuildWorldGridFromLayers()
        {
            if (gridManager.WorldLayerCollection == null) return;

            gridManager.WorldGrid.SuppressEvents = true;
            gridManager.WorldGrid.Clear();

            foreach (var layer in gridManager.WorldLayerCollection.Layers)
            {
                if (layer == null || layer.PreviewTexture == null) continue;

                for (var x = 0; x < layer.PreviewTexture.width; x++)
                for (var y = 0; y < layer.PreviewTexture.height; y++)
                    if (layer.IsPixelPainted(x, y))
                        gridManager.WorldGrid.PlaceTile(x, y, layer);
            }

            gridManager.WorldGrid.SuppressEvents = false;
            gridManager.RefreshAllVisualGrids();
        }

        private GUIContent cachedToolbarIcon;

        public override GUIContent toolbarIcon
        {
            get
            {
                if (cachedToolbarIcon == null)
                    cachedToolbarIcon = new GUIContent(
                        EditorGUIUtility.IconContent("Grid.PaintTool").image,
                        "Grid Painter Tool (Shift+G)"
                    );
                return cachedToolbarIcon;
            }
        }


        private void BeginPaintStroke(string operationName)
        {
            Undo.SetCurrentGroupName(operationName);
            undoGroup = Undo.GetCurrentGroup();
            modifiedLayersInCurrentStroke.Clear();
        }


        private void EndPaintStroke()
        {
            if (modifiedLayersInCurrentStroke.Count > 0)
            {
                foreach (var layer in modifiedLayersInCurrentStroke) layer.SerializeTextureData();

                Undo.CollapseUndoOperations(undoGroup);

                foreach (var layer in modifiedLayersInCurrentStroke) EditorUtility.SetDirty(layer);

                Debug.Log(
                    $"[GridPainterTool] Paint stroke complete - serialized {modifiedLayersInCurrentStroke.Count} layer(s)");
            }

            modifiedLayersInCurrentStroke.Clear();
        }

        private void SerializeAllModifiedLayers()
        {
            if (modifiedLayersInCurrentStroke.Count > 0)
            {
                Debug.Log(
                    $"[GridPainterTool] Serializing {modifiedLayersInCurrentStroke.Count} layer(s) on tool deactivation");

                foreach (var layer in modifiedLayersInCurrentStroke)
                {
                    layer.SerializeTextureData();
                    EditorUtility.SetDirty(layer);
                }

                modifiedLayersInCurrentStroke.Clear();
            }
        }

        private void RecordLayerForUndo(WorldLayer layer)
        {
            if (layer != null && !modifiedLayersInCurrentStroke.Contains(layer))
            {
                Undo.RecordObject(layer, "Paint Tiles");
                modifiedLayersInCurrentStroke.Add(layer);
            }
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!(window is SceneView sceneView))
                return;

            if (sceneView.TryGetOverlay("Grid Painter", out var overlay))
            {
                var painterOverlay = overlay as GridPainterOverlay;
                if (painterOverlay != null)
                {
                    gridManager = painterOverlay.GetActiveGridManager();
                    selectedLayer = painterOverlay.GetSelectedLayer();
                    selectedFluidType = painterOverlay.GetSelectedFluidType();
                    paintMode = painterOverlay.GetPaintMode();
                    brushSize = painterOverlay.GetBrushSize();
                    fluidAmount = painterOverlay.GetFluidAmount();
                    placeAsSource = painterOverlay.GetPlaceAsSource();
                }
            }

            if (gridManager == null) return;

            HandleInput();
            DrawGridPreview();
        }

        private void HandleInput()
        {
            var e = Event.current;
            var controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (e.alt) return;

            if (e.type == EventType.ScrollWheel && e.control)
            {
                brushSize = Mathf.Max(1, brushSize - (int)Mathf.Sign(e.delta.y));
                e.Use();
                SceneView.RepaintAll();
                return;
            }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0)
                    {
                        isPainting = true;
                        BeginPaintStroke("Paint Tiles");
                        PaintAtMousePosition(e);
                        e.Use();
                        GUIUtility.hotControl = controlID;
                    }
                    else if (e.button == 1)
                    {
                        isErasing = true;
                        BeginPaintStroke("Erase Tiles");
                        EraseAtMousePosition(e);
                        e.Use();
                        GUIUtility.hotControl = controlID;
                    }
                    break;

                case EventType.MouseDrag:
                    if (isPainting && e.button == 0)
                    {
                        PaintAtMousePosition(e);
                        e.Use();
                    }
                    else if (isErasing && e.button == 1)
                    {
                        EraseAtMousePosition(e);
                        e.Use();
                    }

                    break;

                case EventType.MouseUp:
                    if (e.button == 0)
                    {
                        isPainting = false;
                        EndPaintStroke();
                        GUIUtility.hotControl = 0;
                    }
                    else if (e.button == 1)
                    {
                        isErasing = false;
                        EndPaintStroke();
                        GUIUtility.hotControl = 0;
                    }

                    break;

                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlID);
                    break;
            }
        }

        /// <summary>
        /// Returns all grid positions within the circular brush radius.
        /// </summary>
        private IEnumerable<Vector2Int> GetBrushPositions(Vector2Int center)
        {
            var radius = brushSize - 1;
            var radiusSq = (radius + 0.5f) * (radius + 0.5f);

            for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
                if (x * x + y * y <= radiusSq)
                {
                    var pos = new Vector2Int(center.x + x, center.y + y);
                    if (gridManager.WorldGrid.IsValidPosition(pos))
                        yield return pos;
                }
        }

        private void PaintAtMousePosition(Event e)
        {
            if (paintMode == PaintMode.Terrain)
                PaintTerrainAtMousePosition(e);
            else
                PaintFluidAtMousePosition(e);
        }

        private void EraseAtMousePosition(Event e)
        {
            if (paintMode == PaintMode.Terrain)
                EraseTerrainAtMousePosition(e);
            else
                EraseFluidAtMousePosition(e);
        }

        private void PaintTerrainAtMousePosition(Event e)
        {
            if (selectedLayer == null || !selectedLayer.IsEnabled || selectedLayer.LockFromPaint)
                return;

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (!gridPos.HasValue) return;

            foreach (var pos in GetBrushPositions(gridPos.Value))
                PaintTile(pos.x, pos.y);
        }

        private void EraseTerrainAtMousePosition(Event e)
        {
            if (selectedLayer == null)
                return;

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (!gridPos.HasValue) return;

            foreach (var pos in GetBrushPositions(gridPos.Value))
                EraseTile(pos.x, pos.y);
        }


        private void PaintFluidAtMousePosition(Event e)
        {
            if (selectedFluidType == null)
            {
                Debug.LogWarning("[GridPainterTool] No fluid type selected!");
                return;
            }

            if (gridManager.FluidSimulator == null)
            {
                Debug.LogWarning("[GridPainterTool] FluidSimulator not initialized on GridManager");
                return;
            }

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (!gridPos.HasValue)
            {
                Debug.LogWarning("[GridPainterTool] Could not get grid position from mouse");
                return;
            }

            var hoveredPlacementLevel = GetHoveredFluidPlacementLevel(e);

            foreach (var pos in GetBrushPositions(gridPos.Value))
            {
                var level = hoveredPlacementLevel ??
                            GetFluidPlacementLevel(pos.x, pos.y);

                if (level < 0 || level >= gridManager.MaxLevels) continue;

                PaintFluid(pos.x, pos.y, level);
            }
        }

        /// <summary>
        /// Finds the level at which to place fluid in a column.
        /// Walks up from the first level above terrain through existing fluid tiles.
        /// Returns the first non-full or empty level, so repeated clicks stack water upward.
        /// Returns -1 if the column is completely full.
        /// </summary>
        private int GetFluidPlacementLevel(int x, int y)
        {
            var terrainLevel = GetTerrainLevelAt(x, y);
            var level = terrainLevel + 1;

            while (level < gridManager.MaxLevels)
            {
                if (!gridManager.WorldGrid.HasFluid(x, y, level))
                    return level;

                var tile = gridManager.WorldGrid.GetTile(x, y, level);
                if (tile?.Fluid != null && !tile.Fluid.IsFull)
                    return level;

                level++;
            }

            return -1;
        }

        private int? GetHoveredFluidPlacementLevel(Event e)
        {
            var picked = HandleUtility.PickGameObject(e.mousePosition, false);
            if (picked == null)
                return null;

            if (!IsVisualTileObject(picked.transform))
                return null;

            if (IsUnderContainer(picked.transform, "Fluid_Visuals"))
                return null;

            var hoveredSolidLevel = Mathf.RoundToInt(picked.transform.position.y);
            var placementLevel = hoveredSolidLevel + 1;

            return Mathf.Clamp(placementLevel, 0, gridManager.MaxLevels - 1);
        }

        private static bool IsVisualTileObject(Transform t)
        {
            while (t != null)
            {
                if (t.name.StartsWith("VisualTile_"))
                    return true;
                t = t.parent;
            }

            return false;
        }

        private static bool IsUnderContainer(Transform t, string containerName)
        {
            while (t != null)
            {
                if (t.name == containerName)
                    return true;
                t = t.parent;
            }

            return false;
        }

        private void EraseFluidAtMousePosition(Event e)
        {
            if (gridManager.FluidSimulator == null)
                return;

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (!gridPos.HasValue) return;

            foreach (var pos in GetBrushPositions(gridPos.Value))
                for (var level = 0; level < 10; level++)
                    EraseFluid(pos.x, pos.y, level);
        }

        private int GetTerrainLevelAt(int x, int y)
        {
            var highestLevel = -1;

            for (var level = 0; level < gridManager.MaxLevels; level++)
            {
                var tile = gridManager.GetWorldTile(x, y, level);
                if (tile != null && tile.SourceLayer != null)
                    highestLevel = level;
            }

            return highestLevel;
        }

        private void PaintFluid(int x, int y, int level)
        {
            gridManager.FluidSimulator.AddFluid(x, y, level, selectedFluidType, fluidAmount, placeAsSource);
            SceneView.RepaintAll();
        }

        private void EraseFluid(int x, int y, int level)
        {
            gridManager.FluidSimulator.RemoveFluid(x, y, level);
            SceneView.RepaintAll();
        }

        private Vector2Int? GetGridPositionFromMouse(Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var gridPlane = new Plane(Vector3.up, Vector3.zero);

            if (gridPlane.Raycast(ray, out var enter))
            {
                var worldPos = ray.GetPoint(enter);
                var gridPos = gridManager.WorldToGridPosition(worldPos);

                if (gridManager.WorldGrid.IsValidPosition(gridPos))
                    return gridPos;
            }

            return null;
        }

        private void PaintTile(int x, int y)
        {
            RecordLayerForUndo(selectedLayer);

            selectedLayer.PaintPixel(x, y, true);
            gridManager.WorldGrid.PlaceTile(x, y, selectedLayer);

            SceneView.RepaintAll();
        }

        private void EraseTile(int x, int y)
        {
            var level = selectedLayer.LayerLevel;
            var tile = gridManager.WorldGrid.GetTile(x, y, level);

            if (tile != null && tile.SourceLayer == selectedLayer)
            {
                RecordLayerForUndo(selectedLayer);

                selectedLayer.PaintPixel(x, y, false);
                EditorUtility.SetDirty(selectedLayer);

                gridManager.WorldGrid.RemoveTile(x, y, level);
                SceneView.RepaintAll();
            }
        }

        private void DrawGridPreview()
        {
            if (gridManager == null || gridManager.WorldGrid == null)
                return;

            var hoveredPos = GetGridPositionFromMouse(Event.current.mousePosition);
            if (hoveredPos.HasValue)
            {
                if (paintMode == PaintMode.Terrain)
                    Handles.color = isPainting ? Color.green : isErasing ? Color.red : Color.yellow;
                else
                    Handles.color = isPainting ? Color.cyan : isErasing ? Color.red : Color.blue;

                foreach (var pos in GetBrushPositions(hoveredPos.Value))
                {
                    var worldPos = gridManager.GridToWorldPosition(pos.x, pos.y);
                    Handles.DrawWireCube(worldPos, Vector3.one * gridManager.TileSize * 0.95f);
                }

                if (!isPainting && !isErasing)
                {
                    var centerWorldPos = gridManager.GridToWorldPosition(hoveredPos.Value.x, hoveredPos.Value.y);
                    string label;

                    if (paintMode == PaintMode.Terrain)
                    {
                        label = selectedLayer != null ? $"Paint: {selectedLayer.LayerName}" : "No layer selected";
                    }
                    else
                    {
                        var fluidTiles = gridManager.WorldGrid.GetAllFluidTiles();
                        if (fluidTiles != null)
                        {
                            var hoveredFluidTiles = fluidTiles.Where(tile =>
                                tile.GridPosition == hoveredPos.Value && tile.HasFluid);

                            foreach (var tile in hoveredFluidTiles)
                            {
                                label = selectedFluidType != null
                                    ? $"Paint: {selectedFluidType.FluidName} ({tile.Fluid.FillAmount}/7){(tile.Fluid.IsSource ? " [SOURCE]" : "")}"
                                    : "No fluid type selected";
                                Handles.Label(centerWorldPos + Vector3.up * 0.5f, label,
                                    EditorStyles.helpBox);
                            }
                        }
                    }
                }
            }
        }
    }
}

