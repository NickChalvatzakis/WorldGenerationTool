using System.Collections.Generic;
using System.Linq;
using CozyWorldGeneration.Core.DualGrid;
using CozyWorldGeneration.Core.Events;
using CozyWorldGeneration.Data.Fluids;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CozyWorldGeneration.Core.Fluids
{
    [ExecuteAlways]
    public class FluidSimulator : MonoBehaviour
    {
        [Header("References")] private GridManager gridManager;
        public FluidGrid fluidGrid;
        public int BodyCount => fluidBodies?.Count ?? 0; // no...don't..don't make the joke.


        [Header("Settings")] [SerializeField] private float tickRate;
        [SerializeField] private bool simulateInEditor = false;


        [Header("Runtime State")] [SerializeField]
        private List<FluidBody> fluidBodies = new();

        private int nextBodyId;
        private float tickTimer;
        private WorldGrid WorldGrid => gridManager?.WorldGrid;


        public FluidGrid FluidGrid
        {
            get
            {
                if (fluidGrid == null && gridManager != null)
                    fluidGrid = new FluidGrid(gridManager.Width, gridManager.Height, gridManager.MaxLevels);
                return fluidGrid;
            }
        }


        private void Update()
        {
            if (fluidGrid == null) return;

            tickTimer += Time.deltaTime;

            if (!(tickTimer >= 1f / tickRate)) return;
            tickTimer = 0f;
            SimulateTick();
        }


        public void Initialize(int width, int height, int maxLevels)
        {
            gridManager = FindAnyObjectByType<GridManager>();
            fluidGrid = new FluidGrid(width, height, maxLevels);
        }

        public void SimulateTick()
        {
            if (fluidGrid.GetTileCount() == 0) return;

            RefillSources();
            FindConnectedBodies();
            EqualizeBodies();
            CheckSettling();
            SpreadBodies();
            CleanupEmptyTiles();
            UpdateFlowDirections();

            if (!fluidGrid.SuppressEvents) ToolEvents.TriggerFluidSimulationTick();
        }

        private void UnsettleBody(FluidBody body)
        {
            body.Unsettle();

            if (!WorldGrid.SuppressEvents)
                ToolEvents.TriggerFluidBodyUnsettled(body);
        }


        /// <summary>
        /// Sets the non-settled tiles that belong to a body with source to have a flow direction from that source
        /// </summary>
        private void UpdateFlowDirections()
        {
            foreach (var body in fluidBodies)
            {
                // Settled bodies have no flow
                if (body.IsSettled)
                {
                    foreach (var tile in body.Tiles.Values) tile.FlowDirection = Vector2.zero;
                    continue;
                }

                // No source = no persistent flow
                if (!body.HasSource)
                {
                    foreach (var tile in body.Tiles.Values) tile.FlowDirection = Vector2.zero;
                    continue;
                }

                // Calculate flow from sources
                UpdateFlowForBody(body);
            }
        }

        /// <summary>
        /// Finds the nearest source and sets the FlowDirection of the tiles in a body
        /// to the direction from the source.
        /// </summary>
        /// <param name="body"></param>
        private void UpdateFlowForBody(FluidBody body)
        {
            var sources = body.Tiles
                .Where(kvp => kvp.Value.IsSource)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var kvp in body.Tiles)
            {
                var position = kvp.Key;
                var tile = kvp.Value;

                if (tile.IsSource)
                {
                    tile.FlowDirection = Vector2.zero;
                    continue;
                }

                // Find nearest source
                var nearestSource = sources[0];
                var nearestDist = float.MaxValue;

                foreach (var source in sources)
                {
                    var dist = Vector3Int.Distance(position, source);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestSource = source;
                    }
                }

                // Flow direction = away from source (horizontal only)
                var dir = new Vector2(
                    position.x - nearestSource.x,
                    position.y - nearestSource.y
                );

                tile.FlowDirection = dir.magnitude > 0 ? dir.normalized : Vector2.zero;
            }
        }

        private void CleanupEmptyTiles()
        {
            var tilesToRemove = new List<Vector3Int>();

            foreach (var position in fluidGrid.GetAllPositions())
            {
                var tile = fluidGrid.GetTile(position);

                // Keep sources  if empty
                if (tile.IsSource) continue;
                if (tile.IsEmpty) tilesToRemove.Add(position);
            }

            foreach (var pos in tilesToRemove) fluidGrid.RemoveFluid(pos.x, pos.y, pos.z);
        }

        private void SpreadBodies()
        {
            var tilesToAdd = new List<(Vector3Int pos, FluidType type, int amount)>();
            foreach (var body in fluidBodies)
            {
                if (body.IsSettled) continue;
                foreach (var kvp in body.Tiles)
                {
                    var position = kvp.Key;
                    var tile = kvp.Value;

                    if (tile.FillAmount <= 1) continue;

                    var spreadPositions = fluidGrid.GetSpreadPositions(
                        position.x, position.y, position.z, WorldGrid);
                    if (spreadPositions.Count == 0) continue;
                    spreadPositions = spreadPositions.OrderBy(p => p.z).ToList();

                    foreach (var targetPos in spreadPositions)
                    {
                        var spreadAmount = CalculateSpreadAmount(tile, targetPos, position);
                        if (spreadAmount > 0)
                        {
                            tilesToAdd.Add((targetPos, tile.Type, spreadAmount));
                            tile.RemoveFillAmount(spreadAmount);
                        }

                        if (tile.FillAmount <= 1) break;
                    }
                }
            }

            foreach (var (pos, type, amount) in tilesToAdd) fluidGrid.PlaceFluid(pos.x, pos.y, pos.z, type, amount);
        }

        private int CalculateSpreadAmount(FluidTile tile, Vector3Int targetPos, Vector3Int position)
        {
            var isBelow = targetPos.z < position.z;
            if (isBelow)
                return Mathf.Min(tile.FillAmount - 1, tile.Type.SpreadRate);
            else
                return Mathf.Min(tile.FillAmount / 2, tile.Type.SpreadRate);
        }

        private void CheckSettling()
        {
            foreach (var body in fluidBodies)
            {
                if (body.HasSource) continue;
                if (body.IsSettled) continue;
                if (!body.Type.CanSettle) continue;

                if (body.AverageFillAmount <= body.Type.SettlingThreshold)
                {
                    body.Settle();
                    if (WorldGrid.SuppressEvents) ToolEvents.TriggerFluidBodySettled(body);
                }
            }
        }

        /// <summary>
        /// Redistribute fluid within each body
        /// </summary>
        private void EqualizeBodies()
        {
            foreach (var fluidBody in fluidBodies)
                EqualizeBody(fluidBody);
        }

        /// <summary>
        /// Equalizes the FillAmount of each level in a body.
        /// <para>Calculates capacity by the cells in each level.
        /// Assigns 7 if the remainingVolume is higher than the capacity.
        /// Assigns remainingVolume divided by how many tiles are at that level,
        /// if volume is lower and keeps the remainder gets added to the first non-full tile.</para>
        /// </summary>
        private void EqualizeBody(FluidBody fluidBody)
        {
            var volume = fluidBody.TotalVolume;
            var levels = fluidBody.Tiles
                .GroupBy(kvp => kvp.Key.z)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var tile in fluidBody.Tiles.Values) tile.FillAmount = 0;

            var remainingVolume = volume;

            foreach (var levelGroup in levels)
            {
                var tilesAtLevel = levelGroup.Select(kvp => kvp.Value).ToList();
                var capacity = tilesAtLevel.Count * 7;

                if (remainingVolume >= capacity)
                {
                    foreach (var tile in tilesAtLevel) tile.FillAmount = 7;
                    remainingVolume -= capacity;
                }
                else if (remainingVolume > 0)
                {
                    var perTile = remainingVolume / tilesAtLevel.Count;
                    var remainder = remainingVolume % tilesAtLevel.Count;

                    foreach (var tile in tilesAtLevel) tile.FillAmount = perTile;

                    for (var i = 0; i < remainder; i++) tilesAtLevel[i].AddFillAmount(1);

                    remainingVolume = 0;
                }

                if (remainingVolume == 0) break;
            }
        }

        /// <summary>
        /// Groups all fluid tiles into FluidBody instances
        /// </summary>
        private void FindConnectedBodies()
        {
            fluidBodies.Clear();
            nextBodyId = 0;

            foreach (var tile in fluidGrid.GetAllTiles()) tile.BodyId = -1;
            foreach (var position in fluidGrid.GetAllPositions())
            {
                var tile = fluidGrid.GetTile(position);
                if (tile.BodyId != -1) continue;
                var body = FloodFillBody(position, tile);
                fluidBodies.Add(body);
            }
        }

        /// <summary>
        /// BFS to find all nearby fluid tiles  and add them to a FluidBody (group)
        /// </summary>
        /// <param name="position"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        private FluidBody FloodFillBody(Vector3Int position, FluidTile tile)
        {
            var body = new FluidBody(nextBodyId++, tile.Type);
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(position);
            var hasSettled = false;
            var hasUnsettled = false;

            while (queue.Count > 0)
            {
                var currentPos = queue.Dequeue();
                var currentTile = fluidGrid.GetTile(currentPos);

                if (currentTile == null) continue;
                if (currentTile.BodyId != -1) continue;
                if (currentTile.Type != body.Type) continue; // Can't connect different fluids

                body.AddTile(currentPos, currentTile);

                if (currentTile.IsSettled) hasSettled = true;
                else hasUnsettled = true;

                var neighbours = fluidGrid.GetAllCardinalNeighbours(currentPos);
                foreach (var neighbourPos in neighbours)
                    if (CanFluidConnect(currentPos, neighbourPos))
                        queue.Enqueue(neighbourPos);
            }

            // if there is at least one unsettled tile
            if (hasSettled && hasUnsettled) UnsettleBody(body);
            return body;
        }

        private bool CanFluidConnect(Vector3Int to, Vector3Int from)
        {
            if (!fluidGrid.IsValidPosition(to)) return false;
            if (!fluidGrid.HasTile(to)) return false;
            if (WorldGrid.HasTileAt(to.x, to.y, to.z)) return false;
            return true;
        }

        private void RefillSources()
        {
            foreach (var tile in fluidGrid.GetAllTiles())
                if (tile.IsSource)
                    tile.FillAmount = 7;
        }

        public void AddFluid(int x, int y, int level, FluidType type, int amount, bool isSource)
        {
            var tile = fluidGrid.GetTile(x, y, level);
            fluidGrid.PlaceFluid(x, y, level, type, amount);
            SetSource(x, y, level, isSource);
        }

        public void RemoveFluid(int x, int y, int level)
        {
            fluidGrid.RemoveFluid(x, y, level);
        }

        public void SetSource(int x, int y, int level, bool isSource)
        {
            var tile = fluidGrid.GetTile(x, y, level);
            tile.IsSource = isSource;
            if (isSource && tile.IsSettled)
            {
                var body = GetFluidBodyAt(x, y, level);
                if (body != null) UnsettleBody(body);
            }
        }

        public FluidBody GetFluidBodyAt(int x, int y, int level)
        {
            var tile = fluidGrid.GetTile(x, y, level);
            if (tile == null || tile.BodyId == -1) return null;
            return fluidBodies.Find(body => body.BodyId == tile.BodyId);
        }
#if UNITY_EDITOR
        private void OnEnable()
        {
            EditorApplication.update += EditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= EditorUpdate;
        }

        private void EditorUpdate()
        {
            // Rebuild bodies in editor for display purposes (even without full simulation)
            if (!Application.isPlaying && FluidGrid != null && FluidGrid.GetTileCount() > 0)
                // Only rebuild bodies, don't run full simulation unless enabled
                if (!simulateInEditor)
                    FindConnectedBodies();
        }

#endif
    }
}