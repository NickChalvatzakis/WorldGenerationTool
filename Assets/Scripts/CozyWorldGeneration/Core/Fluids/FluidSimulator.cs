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
        public int BodyCount => fluidBodies?.Count ?? 0;


        [Header("Settings")] [SerializeField] private float tickRate;
        [SerializeField] private bool simulateInEditor = false;


        [Header("Runtime State")] [SerializeField]
        private List<FluidBody> fluidBodies = new();

        private int nextBodyId;
        private float tickTimer;
        private WorldGrid WorldGrid => gridManager?.WorldGrid;


        private void Update()
        {
            if (WorldGrid == null) return;

            tickTimer += Time.deltaTime;

            if (!(tickTimer >= 1f / tickRate)) return;
            tickTimer = 0f;
            SimulateTick();
        }


        public void Initialize(GridManager manager)
        {
            gridManager = manager;
        }

        public void SimulateTick()
        {
            if (WorldGrid == null) return;
            if (!WorldGrid.GetAllFluidTiles().Any()) return;

            RefillSources();
            FindConnectedBodies();
            EqualizeBodies();
            CheckSettling();
            SpreadBodies();
            CleanupEmptyTiles();
            UpdateFlowDirections();

            if (!WorldGrid.SuppressEvents) ToolEvents.TriggerFluidSimulationTick();
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
                    foreach (var kvp in body.Tiles)
                    {
                        var tile = WorldGrid.GetTile(kvp.Key);
                        if (tile?.Fluid != null)
                            tile.Fluid.FlowDirection = Vector2.zero;
                    }

                    continue;
                }

                // No source = no persistent flow
                if (!body.HasSource)
                {
                    foreach (var kvp in body.Tiles)
                    {
                        var tile = WorldGrid.GetTile(kvp.Key);
                        if (tile?.Fluid != null)
                            tile.Fluid.FlowDirection = Vector2.zero;
                    }

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
        private void UpdateFlowForBody(FluidBody body)
        {
            var sources = body.Tiles
                .Where(kvp => kvp.Value.IsSource)
                .Select(kvp => kvp.Key)
                .ToList();

            if (sources.Count == 0) return;

            foreach (var kvp in body.Tiles)
            {
                var position = kvp.Key;
                var fluidData = kvp.Value;

                var tile = WorldGrid.GetTile(position);
                if (tile?.Fluid == null) continue;

                if (fluidData.IsSource)
                {
                    tile.Fluid.FlowDirection = Vector2.zero;
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

                tile.Fluid.FlowDirection = dir.magnitude > 0 ? dir.normalized : Vector2.zero;
            }
        }

        private void CleanupEmptyTiles()
        {
            var tilesToRemove = new List<Vector3Int>();

            foreach (var position in WorldGrid.GetAllPositions())
            {
                var tile = WorldGrid.GetTile(position);
                if (tile?.Fluid == null) continue;

                // Keep sources even if empty
                if (tile.Fluid.IsSource) continue;

                if (tile.Fluid.IsEmpty)
                    tilesToRemove.Add(position);
            }

            foreach (var pos in tilesToRemove)
                WorldGrid.RemoveFluid(pos.x, pos.y, pos.z);
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
                    var fluidData = kvp.Value;

                    if (fluidData.FillAmount <= 1) continue;

                    var spreadPositions = WorldGrid.GetFluidSpreadPositions(
                        position.x, position.y, position.z);

                    if (spreadPositions.Count == 0) continue;

                    spreadPositions = spreadPositions.OrderBy(p => p.z).ToList();

                    foreach (var targetPos in spreadPositions)
                    {
                        var spreadAmount = CalculateSpreadAmount(fluidData, targetPos, position);
                        if (spreadAmount > 0)
                        {
                            tilesToAdd.Add((targetPos, fluidData.Type, spreadAmount));
                            fluidData.RemoveFillAmount(spreadAmount);
                        }

                        if (fluidData.FillAmount <= 1) break;
                    }
                }
            }

            foreach (var (pos, type, amount) in tilesToAdd)
                WorldGrid.PlaceFluid(pos.x, pos.y, pos.z, type, amount);
        }

        private int CalculateSpreadAmount(FluidData fluidData, Vector3Int targetPos, Vector3Int position)
        {
            var isBelow = targetPos.z < position.z;
            if (isBelow)
                return Mathf.Min(fluidData.FillAmount - 1, fluidData.Type.SpreadRate);
            else
                return Mathf.Min(fluidData.FillAmount / 2, fluidData.Type.SpreadRate);
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
                    if (!WorldGrid.SuppressEvents)
                        ToolEvents.TriggerFluidBodySettled(body);
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
        /// Calculates capacity by the cells in each level.
        /// Assigns 7 if the remainingVolume is higher than the capacity.
        /// Assigns remainingVolume divided by how many tiles are at that level,
        /// if volume is lower and keeps the remainder gets added to the first non-full tile.
        /// </summary>
        private void EqualizeBody(FluidBody fluidBody)
        {
            var volume = fluidBody.TotalVolume;
            var levels = fluidBody.Tiles
                .GroupBy(kvp => kvp.Key.z)
                .OrderBy(g => g.Key)
                .ToList();

            // Reset all fill amounts
            foreach (var kvp in fluidBody.Tiles)
                kvp.Value.FillAmount = 0;

            var remainingVolume = volume;

            foreach (var levelGroup in levels)
            {
                var fluidDataList = levelGroup.Select(kvp => kvp.Value).ToList();
                var capacity = fluidDataList.Count * 7;

                if (remainingVolume >= capacity)
                {
                    foreach (var fluidData in fluidDataList)
                        fluidData.FillAmount = 7;
                    remainingVolume -= capacity;
                }
                else if (remainingVolume > 0)
                {
                    var perTile = remainingVolume / fluidDataList.Count;
                    var remainder = remainingVolume % fluidDataList.Count;

                    foreach (var fluidData in fluidDataList)
                        fluidData.FillAmount = perTile;

                    for (var i = 0; i < remainder; i++)
                        fluidDataList[i].AddFillAmount(1);

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

            // Reset all body IDs
            foreach (var tile in WorldGrid.GetAllFluidTiles())
                if (tile.Fluid != null)
                    tile.Fluid.BodyId = -1;

            // Find connected bodies
            foreach (var position in WorldGrid.GetAllPositions())
            {
                var tile = WorldGrid.GetTile(position);
                if (tile?.Fluid == null) continue;
                if (tile.Fluid.BodyId != -1) continue;

                var body = FloodFillBody(position, tile.Fluid);
                fluidBodies.Add(body);
            }
        }

        /// <summary>
        /// BFS to find all nearby fluid tiles and add them to a FluidBody (group)
        /// </summary>
        private FluidBody FloodFillBody(Vector3Int position, FluidData startFluid)
        {
            var body = new FluidBody(nextBodyId++, startFluid.Type);
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(position);
            var hasSettled = false;
            var hasUnsettled = false;

            while (queue.Count > 0)
            {
                var currentPos = queue.Dequeue();
                var currentTile = WorldGrid.GetTile(currentPos);
                var currentFluid = currentTile?.Fluid;

                if (currentFluid == null) continue;
                if (currentFluid.BodyId != -1) continue;
                if (currentFluid.Type != body.Type) continue; // Can't connect different fluids

                body.AddTile(currentPos, currentFluid);

                if (currentFluid.IsSettled) hasSettled = true;
                else hasUnsettled = true;

                var neighbours = WorldGrid.GetAllCardinalNeighbours(currentPos);
                foreach (var neighbourPos in neighbours)
                    if (CanFluidConnect(currentPos, neighbourPos))
                        queue.Enqueue(neighbourPos);
            }

            // If there is at least one unsettled tile, unsettle the whole body
            if (hasSettled && hasUnsettled)
                UnsettleBody(body);

            return body;
        }

        private bool CanFluidConnect(Vector3Int from, Vector3Int to)
        {
            if (!WorldGrid.IsValidPosition(to)) return false;
            if (!WorldGrid.HasFluid(to.x, to.y, to.z)) return false;
            if (WorldGrid.HasSolidTile(to.x, to.y, to.z)) return false;
            return true;
        }

        private void RefillSources()
        {
            foreach (var tile in WorldGrid.GetAllFluidTiles())
                if (tile.Fluid != null && tile.Fluid.IsSource)
                    tile.Fluid.FillAmount = 7;
        }

        #region Public API

        public void AddFluid(int x, int y, int level, FluidType type, int amount, bool isSource)
        {
            WorldGrid.PlaceFluid(x, y, level, type, amount);
            SetSource(x, y, level, isSource);
        }

        public void RemoveFluid(int x, int y, int level)
        {
            WorldGrid.RemoveFluid(x, y, level);
        }

        public void SetSource(int x, int y, int level, bool isSource)
        {
            var tile = WorldGrid.GetTile(x, y, level);
            if (tile?.Fluid == null) return;

            tile.Fluid.IsSource = isSource;

            if (isSource && tile.Fluid.IsSettled)
            {
                var body = GetFluidBodyAt(x, y, level);
                if (body != null) UnsettleBody(body);
            }
        }

        public FluidBody GetFluidBodyAt(int x, int y, int level)
        {
            var tile = WorldGrid.GetTile(x, y, level);
            if (tile?.Fluid == null || tile.Fluid.BodyId == -1) return null;

            return fluidBodies.Find(body => body.BodyId == tile.Fluid.BodyId);
        }

        #endregion

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
            if (!Application.isPlaying && WorldGrid != null && WorldGrid.GetAllFluidTiles().Any())
                // Only rebuild bodies, don't run full simulation unless enabled
                if (!simulateInEditor)
                    FindConnectedBodies();
        }
#endif
    }
}