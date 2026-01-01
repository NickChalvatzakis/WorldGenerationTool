using System.Collections.Generic;
using CozyWorldGeneration.Core;
using CozyWorldGeneration.Data.Layers;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace CozyWorldGeneration.Editor
{
    /// <summary>
    /// Scene View tool for painting on the grid.
    /// Handles mouse input and grid interaction.
    /// </summary>
    [EditorTool("Grid Painter Tool")]
    public class GridPainterTool : EditorTool
    {
        private GridManager gridManager;
        private WorldLayer selectedLayer;
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

            if (gridManager.WorldGrid != null) RebuildWorldGridFromLayers();

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
            gridManager.RefreshAlLVisualGrids();
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
            if (modifiedLayers.Count > 0) Undo.CollapseUndoOperations(undoGroup);
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

            // Get the active grid manager and selected layer from overlay
            if (sceneView.TryGetOverlay("Grid Painter", out var overlay))
            {
                var painterOverlay = overlay as GridPainterOverlay;
                if (painterOverlay != null)
                {
                    gridManager = painterOverlay.GetActiveGridManager();
                    selectedLayer = painterOverlay.GetSelectedLayer();
                    brushSize = painterOverlay.GetBrushSize();
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

            // Allow camera controls (Alt + mouse) to pass through
            if (e.alt) return;

            if (e.type == EventType.ScrollWheel && e.control)
            {
                brushSize = Mathf.Max(1, brushSize - (int)Mathf.Sign(e.delta.y));
                e.Use();
                SceneView.RepaintAll();
                return;
            }

            // TODO: change what layer you control / the layer level of the world grid
            // if (e.type == EventType.ScrollWheel && e.shift && e.control)
            // {
            //     gridManager.WorldLayerCollection.Get
            // }

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0) // Left click
                    {
                        isPainting = true;
                        BeginPaintOperation("Paint Tiles");
                        PaintAtMousePosition(e);
                        e.Use();
                        GUIUtility.hotControl = controlID;
                    }
                    else if (e.button == 1) // Right click
                    {
                        isErasing = true;
                        BeginPaintOperation("Erase Tiles");
                        EraseAtMousePosition(e);
                        e.Use();
                        GUIUtility.hotControl = controlID;
                    }

                    break;

                case EventType.MouseDrag:
                    if (isPainting && e.button == 0)
                    {
                        BeginPaintOperation("Paint Tiles");
                        PaintAtMousePosition(e);
                        e.Use();
                    }
                    else if (isErasing && e.button == 1)
                    {
                        BeginPaintOperation("Erase Tiles");
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

        /// <summary>
        /// Gets all grid positions within the brush radius (spherical/circular)
        /// </summary>
        private IEnumerable<Vector2Int> GetBrushPositions(Vector2Int center)
        {
            var radius = brushSize - 1;
            var radiusSq = (radius + 0.5f) * (radius + 0.5f);

            for (var x = -radius; x <= radius; x++)
            for (var y = -radius; y <= radius; y++)
                // Circular check
                if (x * x + y * y <= radiusSq)
                {
                    var pos = new Vector2Int(center.x + x, center.y + y);
                    if (gridManager.WorldGrid.IsValidPosition(pos))
                        yield return pos;
                }
        }


        private void PaintAtMousePosition(Event e)
        {
            if (selectedLayer == null || !selectedLayer.IsEnabled || selectedLayer.LockFromPaint)
                return;

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (!gridPos.HasValue) return;
            foreach (var pos in GetBrushPositions(gridPos.Value))
                PaintTile(pos.x, pos.y);
        }

        private void EraseAtMousePosition(Event e)
        {
            if (selectedLayer == null)
                return;

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (!gridPos.HasValue) return;
            foreach (var pos in GetBrushPositions(gridPos.Value)) EraseTile(pos.x, pos.y);
        }

        private Vector2Int? GetGridPositionFromMouse(Vector2 mousePosition)
        {
            var ray = HandleUtility.GUIPointToWorldRay(mousePosition);
            var gridPlane = new Plane(Vector3.up, Vector3.zero);

            if (gridPlane.Raycast(ray, out var enter))
            {
                var worldPos = ray.GetPoint(enter);
                var gridPos = gridManager.WorldToGridPosition(worldPos);

                if (gridManager.WorldGrid.IsValidPosition(gridPos)) return gridPos;
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

            // Highlight hovered cell
            var hoveredPos = GetGridPositionFromMouse(Event.current.mousePosition);
            if (hoveredPos.HasValue)
            {
                Handles.color = isPainting ? Color.green : isErasing ? Color.red : Color.yellow;
                foreach (var pos in GetBrushPositions(hoveredPos.Value))
                {
                    var worldPos = gridManager.GridToWorldPosition(pos.x, pos.y);
                    Handles.DrawWireCube(worldPos, Vector3.one * gridManager.TileSize * 0.95f);
                }

                // Show what will be painted
                if (selectedLayer != null && !isPainting && !isErasing)
                {
                    var centerWorldPos = gridManager.GridToWorldPosition(hoveredPos.Value.x, hoveredPos.Value.y);
                    Handles.Label(
                        centerWorldPos + Vector3.up * 0.5f,
                        $"Paint: {selectedLayer.LayerName}",
                        EditorStyles.helpBox
                    );
                }
            }
            //
            // var debugConfigDrawTiles = gridManager.DrawDebugTiles;
            //
            // if (!debugConfigDrawTiles) return;
            //
            // // Draw grid cells and tile type labels
            // for (var x = 0; x < gridManager.Width; x++)
            // for (var y = 0; y < gridManager.Height; y++)
            // {
            //     var worldPos = gridManager.GridToWorldPosition(x, y);
            //     var cellSize = Vector3.one * gridManager.TileSize;
            //
            //
            //     // Draw tile if it exists
            //     var tile = gridManager.WorldGrid.GetTile(x, y);
            //     if (tile != null)
            //     {
            //         // Color based on source layer
            //         var tileColor = tile.SourceLayer != null ? tile.SourceLayer.LayerColor : Color.white;
            //         tileColor.a = 0.5f;
            //         Handles.color = tileColor;
            //         Handles.DrawSolidRectangleWithOutline(
            //             new Vector3[]
            //             {
            //                 worldPos + new Vector3(-0.5f, 0, -0.5f) * gridManager.TileSize,
            //                 worldPos + new Vector3(0.5f, 0, -0.5f) * gridManager.TileSize,
            //                 worldPos + new Vector3(0.5f, 0, 0.5f) * gridManager.TileSize,
            //                 worldPos + new Vector3(-0.5f, 0, 0.5f) * gridManager.TileSize
            //             },
            //             tileColor,
            //             Color.clear
            //         );
            //
            //         // Draw tile type label
            //         Handles.Label(
            //             worldPos + Vector3.up * 0.1f,
            //             tile.SourceLayer.name,
            //             new GUIStyle(EditorStyles.whiteBoldLabel)
            //             {
            //                 fontSize = 10,
            //                 alignment = TextAnchor.MiddleCenter,
            //                 normal = { textColor = Color.black }
            //             }
            //         );
            //     }
            // }
        }
    }
}