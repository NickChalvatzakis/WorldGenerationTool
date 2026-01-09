using System.Collections.Generic;
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
        }

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

        private void BeginPaintOperation(string operationName)
        {
            Undo.SetCurrentGroupName(operationName);
            undoGroup = Undo.GetCurrentGroup();
            modifiedLayers.Clear();
        }

        private void EndPaintOperation()
        {
            if (modifiedLayers.Count > 0)
                Undo.CollapseUndoOperations(undoGroup);
            modifiedLayers.Clear();
        }

        private void RecordLayerUndo(WorldLayer layer)
        {
            if (layer != null && !modifiedLayers.Contains(layer))
            {
                Undo.RecordObject(layer, "Paint Tiles");
                modifiedLayers.Add(layer);
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
                        BeginPaintOperation(paintMode == PaintMode.Terrain ? "Paint Tiles" : "Paint Fluid");
                        PaintAtMousePosition(e);
                        e.Use();
                        GUIUtility.hotControl = controlID;
                    }
                    else if (e.button == 1)
                    {
                        isErasing = true;
                        BeginPaintOperation(paintMode == PaintMode.Terrain ? "Erase Tiles" : "Erase Fluid");
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
                        EndPaintOperation();
                        GUIUtility.hotControl = 0;
                    }
                    else if (e.button == 1)
                    {
                        isErasing = false;
                        EndPaintOperation();
                        GUIUtility.hotControl = 0;
                    }

                    break;

                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlID);
                    break;
            }
        }

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
                return;

            if (gridManager.FluidSimulator == null)
            {
                Debug.LogWarning("FluidSimulator not initialized on GridManager");
                return;
            }

            // Add this check
            if (gridManager.FluidSimulator.FluidGrid == null)
            {
                Debug.LogWarning("FluidGrid not initialized. Enter Play mode or call Initialize.");
                return;
            }

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (!gridPos.HasValue) return;

            foreach (var pos in GetBrushPositions(gridPos.Value))
            {
                var level = GetTerrainLevelAt(pos.x, pos.y) + 1;
                PaintFluid(pos.x, pos.y, level);
            }
        }

        private void EraseFluidAtMousePosition(Event e)
        {
            if (gridManager.FluidSimulator == null)
                return;

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (!gridPos.HasValue) return;

            foreach (var pos in GetBrushPositions(gridPos.Value))
                // Try to erase fluid at all levels
                for (var level = 0; level < 10; level++)
                    EraseFluid(pos.x, pos.y, level);
        }

        private int GetTerrainLevelAt(int x, int y)
        {
            var highestLevel = -1;

            foreach (var position in gridManager.WorldGrid.GetAllPositions())
                if (position.x == x && position.y == y)
                    if (position.z > highestLevel)
                        highestLevel = position.z;

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
            RecordLayerUndo(selectedLayer);

            selectedLayer.PaintPixel(x, y, true);
            gridManager.WorldGrid.PlaceTile(x, y, selectedLayer);
            EditorUtility.SetDirty(selectedLayer);
            SceneView.RepaintAll();
        }

        private void EraseTile(int x, int y)
        {
            var level = selectedLayer.LayerLevel;
            var tile = gridManager.WorldGrid.GetTile(x, y, level);

            if (tile != null && tile.SourceLayer == selectedLayer)
            {
                RecordLayerUndo(selectedLayer);

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

            Handles.color = Color.green;

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
                        label = selectedLayer != null ? $"Paint: {selectedLayer.LayerName}" : "No layer selected";
                    else
                        label = selectedFluidType != null
                            ? $"Paint: {selectedFluidType.FluidName} ({fluidAmount}/7){(placeAsSource ? " [SOURCE]" : "")}"
                            : "No fluid type selected";

                    Handles.Label(centerWorldPos + Vector3.up * 0.5f, label, EditorStyles.helpBox);
                }
            }
        }
    }
}