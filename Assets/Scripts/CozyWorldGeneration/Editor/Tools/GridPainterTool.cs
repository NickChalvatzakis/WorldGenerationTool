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

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (e.button == 0) // Left click
                    {
                        isPainting = true;
                        PaintAtMousePosition(e);
                        e.Use();
                        GUIUtility.hotControl = controlID;
                    }
                    else if (e.button == 1) // Right click
                    {
                        isErasing = true;
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
                        GUIUtility.hotControl = 0;
                    }
                    else if (e.button == 1)
                    {
                        isErasing = false;
                        GUIUtility.hotControl = 0;
                    }

                    break;

                case EventType.Layout:
                    HandleUtility.AddDefaultControl(controlID);
                    break;
            }
        }

        private void PaintAtMousePosition(Event e)
        {
            if (selectedLayer == null || !selectedLayer.IsEnabled || selectedLayer.LockFromPaint)
                return;

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (gridPos.HasValue) PaintTile(gridPos.Value.x, gridPos.Value.y);
        }

        private void EraseAtMousePosition(Event e)
        {
            if (selectedLayer == null)
                return;

            var gridPos = GetGridPositionFromMouse(e.mousePosition);
            if (gridPos.HasValue) EraseTile(gridPos.Value.x, gridPos.Value.y);
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
            // Paint on the preview texture
            selectedLayer.PaintPixel(x, y, true);

            // Create or update the world tile
            gridManager.WorldGrid.PlaceTile(x, y, selectedLayer.TileType);

            // Set the source layer for tracking
            var tile = gridManager.WorldGrid.GetTile(x, y);
            if (tile != null) tile.SourceLayer = selectedLayer;

            EditorUtility.SetDirty(selectedLayer);
            SceneView.RepaintAll();
        }

        private void EraseTile(int x, int y)
        {
            // Get the tile to check if it belongs to the selected layer
            var tile = gridManager.WorldGrid.GetTile(x, y);

            if (tile != null && (selectedLayer == null || tile.SourceLayer == selectedLayer))
            {
                // Erase from preview texture
                if (tile.SourceLayer != null)
                {
                    tile.SourceLayer.PaintPixel(x, y, false);
                    EditorUtility.SetDirty(tile.SourceLayer);
                }

                // Remove from world grid
                gridManager.WorldGrid.SetTile(x, y, null);
                SceneView.RepaintAll();
            }
        }

        private void DrawGridPreview()
        {
            if (gridManager == null || gridManager.WorldGrid == null)
                return;

            Handles.color = Color.green;

            // Draw grid cells and tile type labels
            for (var x = 0; x < gridManager.Width; x++)
            for (var y = 0; y < gridManager.Height; y++)
            {
                var worldPos = gridManager.GridToWorldPosition(x, y);
                var cellSize = Vector3.one * gridManager.TileSize;


                // Draw tile if it exists
                var tile = gridManager.WorldGrid.GetTile(x, y);
                if (tile != null)
                {
                    // Color based on source layer
                    var tileColor = tile.SourceLayer != null ? tile.SourceLayer.LayerColor : Color.white;
                    tileColor.a = 0.5f;
                    Handles.color = tileColor;
                    Handles.DrawSolidRectangleWithOutline(
                        new Vector3[]
                        {
                            worldPos + new Vector3(-0.5f, 0, -0.5f) * gridManager.TileSize,
                            worldPos + new Vector3(0.5f, 0, -0.5f) * gridManager.TileSize,
                            worldPos + new Vector3(0.5f, 0, 0.5f) * gridManager.TileSize,
                            worldPos + new Vector3(-0.5f, 0, 0.5f) * gridManager.TileSize
                        },
                        tileColor,
                        Color.clear
                    );

                    // Draw tile type label
                    Handles.Label(
                        worldPos + Vector3.up * 0.1f,
                        tile.Type.ToString(),
                        new GUIStyle(EditorStyles.whiteBoldLabel)
                        {
                            fontSize = 10,
                            alignment = TextAnchor.MiddleCenter,
                            normal = { textColor = Color.black }
                        }
                    );
                }
            }

            // Highlight hovered cell
            var hoveredPos = GetGridPositionFromMouse(Event.current.mousePosition);
            if (hoveredPos.HasValue)
            {
                var worldPos = gridManager.GridToWorldPosition(hoveredPos.Value.x, hoveredPos.Value.y);
                Handles.color = isPainting ? Color.green : isErasing ? Color.red : Color.yellow;
                Handles.DrawWireCube(worldPos, Vector3.one * gridManager.TileSize * 1.1f);

                // Show what will be painted
                if (selectedLayer != null && !isPainting && !isErasing)
                    Handles.Label(
                        worldPos + Vector3.up * 0.5f,
                        $"Paint: {selectedLayer.TileType}",
                        EditorStyles.helpBox
                    );
            }
        }
    }
}