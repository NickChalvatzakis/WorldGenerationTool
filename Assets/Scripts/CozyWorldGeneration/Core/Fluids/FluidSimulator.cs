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
            ApplyGravity(); // pull floating fluid to the first solid surface below
            FindConnectedBodies();
            EqualizeBodies();
            CheckSettling();
            SpreadBodies(); // horizontal surface-only spread
            CreateWaterfalls(); // edge detection + column fall to landing
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

                    if (fluidData.IsWaterfall) continue; // waterfall tiles don't spread horizontally

                    var totalSpread = 0;
                    var startingAmount = fluidData.FillAmount;

                    if (startingAmount <= 1) continue;

                    var spreadPositions = WorldGrid.GetFluidSpreadPositions(position.x, position.y, position.z);
                    if (spreadPositions.Count == 0) continue;

                    foreach (var targetPos in spreadPositions)
                    {
                        var remaining = startingAmount - totalSpread;
                        if (remaining <= 1) break;

                        var spreadAmount = CalculateSpreadAmount(remaining, fluidData.Type.SpreadRate);
                        if (spreadAmount <= 0) continue;

                        tilesToAdd.Add((targetPos, fluidData.Type, spreadAmount));
                        totalSpread += spreadAmount;
                    }

                    if (totalSpread > 0)
                        fluidData.RemoveFillAmount(totalSpread);
                }
            }

            foreach (var (pos, type, amount) in tilesToAdd)
                WorldGrid.PlaceFluid(pos.x, pos.y, pos.z, type, amount);
        }

        private int CalculateSpreadAmount(int remainingAmount, int spreadRate)
        {
            if (remainingAmount <= 1) return 0;
            return Mathf.Min(remainingAmount / 2, spreadRate);
        }

        /// <summary>
        /// Moves any fluid tile that has no solid support below it (and is not a waterfall tile)
        /// to the first solid landing surface in its column.
        /// Fluid with no landing at all (empty column) is removed.
        /// </summary>
        private void ApplyGravity()
        {
            var toMove = new List<(Vector3Int from, int landingLevel, FluidType type, int amount, bool isSource)>();
            var toRemove = new List<Vector3Int>();

            foreach (var position in WorldGrid.GetAllPositions().ToList())
            {
                var tile = WorldGrid.GetTile(position);
                if (tile?.Fluid == null) continue;
                if (tile.Fluid.IsWaterfall) continue; // waterfall tiles are intentionally floating
                if (WorldGrid.HasSolidBelow(position.x, position.y, position.z)) continue; // already supported

                var landingLevel = WorldGrid.FindLandingLevel(position.x, position.y, position.z - 1);
                if (landingLevel < 0)
                {
                    toRemove.Add(position); // no solid anywhere below — remove
                    continue;
                }

                toMove.Add((position, landingLevel, tile.Fluid.Type, tile.Fluid.FillAmount, tile.Fluid.IsSource));
            }

            foreach (var pos in toRemove)
                WorldGrid.RemoveFluid(pos.x, pos.y, pos.z);

            foreach (var (from, landingLevel, type, amount, isSource) in toMove)
            {
                WorldGrid.RemoveFluid(from.x, from.y, from.z);
                WorldGrid.PlaceFluid(from.x, from.y, landingLevel, type, amount);
                if (isSource)
                {
                    var landingTile = WorldGrid.GetTile(from.x, from.y, landingLevel);
                    if (landingTile?.Fluid != null)
                        landingTile.Fluid.IsSource = true;
                }
            }
        }

        /// <summary>
        /// Detects surface fluid tiles (HasSolidBelow == true) that border a drop edge — a
        /// cardinal neighbour at the same level with no solid below it.
        /// For each such edge, pours fluid straight down to the first solid landing surface.
        /// Volume is conserved: the source tile is reduced by the amount poured.
        /// </summary>
        private void CreateWaterfalls()
        {
            // landingPos -> (type, accumulated amount)
            var placements = new Dictionary<Vector3Int, (FluidType type, int amount)>();
            // sourcePos -> total volume taken this tick
            var reductions = new Dictionary<Vector3Int, int>();

            int[] dx = { 0, 1, 0, -1 };
            int[] dy = { 1, 0, -1, 0 };

            foreach (var position in WorldGrid.GetAllPositions().ToList())
            {
                var tile = WorldGrid.GetTile(position);
                if (tile?.Fluid == null) continue;
                if (tile.Fluid.IsWaterfall) continue;
                if (!WorldGrid.HasSolidBelow(position.x, position.y, position.z)) continue;

                var fill = tile.Fluid.FillAmount;
                if (fill <= 1) continue;

                for (var i = 0; i < 4; i++)
                {
                    var nx = position.x + dx[i];
                    var ny = position.y + dy[i];
                    var level = position.z;

                    if (!WorldGrid.IsValidPosition(nx, ny)) continue;
                    if (WorldGrid.HasSolidBelow(nx, ny, level)) continue; // neighbour has a floor — not a drop edge
                    if (WorldGrid.HasFluid(nx, ny, level)) continue; // edge position already occupied

                    var landingLevel = WorldGrid.FindLandingLevel(nx, ny, level - 1);
                    if (landingLevel < 0) continue; // no solid anywhere in that column

                    var spreadAmount = Mathf.Min(fill - 1, tile.Fluid.Type.SpreadRate);
                    if (spreadAmount <= 0) continue;

                    var landingPos = new Vector3Int(nx, ny, landingLevel);

                    // Accumulate from multiple sources pouring into the same landing spot
                    if (placements.TryGetValue(landingPos, out var existing))
                        placements[landingPos] = (existing.type, Mathf.Min(existing.amount + spreadAmount, 7));
                    else
                        placements[landingPos] = (tile.Fluid.Type, spreadAmount);

                    // Track how much this source tile is giving up
                    reductions.TryGetValue(position, out var currentReduction);
                    reductions[position] = currentReduction + spreadAmount;
                }
            }

            // Volume conservation: drain the source tiles (leave at least 1 unit so they don't vanish)
            foreach (var kvp in reductions)
            {
                var sourceTile = WorldGrid.GetTile(kvp.Key);
                if (sourceTile?.Fluid != null)
                    sourceTile.Fluid.RemoveFillAmount(Mathf.Min(kvp.Value, sourceTile.Fluid.FillAmount - 1));
            }

            // Place fluid at landing spots — normal tiles that spread freely from there
            foreach (var kvp in placements)
                WorldGrid.PlaceFluid(kvp.Key.x, kvp.Key.y, kvp.Key.z, kvp.Value.type, kvp.Value.amount);
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
            var levels = fluidBody.Tiles
                .GroupBy(kvp => kvp.Key.z)
                .ToList();

            foreach (var levelGroup in levels)
            {
                var levelTiles = levelGroup.Select(kvp => kvp.Value).ToList();
                if (levelTiles.Count == 0) continue;

                var levelVolume = levelTiles.Sum(t => t.FillAmount);

                // Reset this level, then redistribute only within same level.
                foreach (var t in levelTiles)
                    t.FillAmount = 0;

                var perTile = levelVolume / levelTiles.Count;
                var remainder = levelVolume % levelTiles.Count;

                foreach (var t in levelTiles)
                    t.FillAmount = perTile;

                // Keep your frontier preference if you already added it; otherwise this is fine:
                for (var i = 0; i < remainder; i++)
                    levelTiles[i].AddFillAmount(1);
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