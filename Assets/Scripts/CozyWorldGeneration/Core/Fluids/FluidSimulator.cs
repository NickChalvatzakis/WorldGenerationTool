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
    /// <summary>
    /// Runs fluid simulation steps and emits events consumed by fluid visuals.
    /// </summary>
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

        /// <summary>
        /// Executes one simulation tick: refill, gravity, body solve, spread, waterfalls, then flow directions.
        /// </summary>
        public void SimulateTick()
        {
            if (WorldGrid == null) return;
            if (!WorldGrid.GetAllFluidTiles().Any()) return;

            RefillSources();
            ClearWaterfallTiles();
            ApplyGravity();
            FindConnectedBodies();
            EqualizeBodies();
            ApplyPressure();
            CheckSettling();
            SpreadBodies();
            CreateWaterfalls();
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

        private void UpdateFlowDirections()
        {
            foreach (var body in fluidBodies)
            {
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

                UpdateFlowForBody(body);
            }
        }

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

                if (tile.Fluid.IsSource) continue;

                if (tile.Fluid.IsEmpty)
                    tilesToRemove.Add(position);
            }

            foreach (var pos in tilesToRemove)
                WorldGrid.RemoveFluid(pos.x, pos.y, pos.z);
        }

        private void ClearWaterfallTiles()
        {
            var tilesToRemove = new List<Vector3Int>();

            foreach (var position in WorldGrid.GetAllPositions())
            {
                var tile = WorldGrid.GetTile(position);
                if (tile?.Fluid != null && tile.Fluid.IsWaterfall)
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
                    if (fluidData.IsWaterfall) continue;

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
        /// Moves unsupported fluid down to the first valid landing level in the same column.
        /// </summary>
        private void ApplyGravity()
        {
            var toMove = new List<(Vector3Int from, int landingLevel, FluidType type, int amount, bool isSource)>();
            var toRemove = new List<Vector3Int>();

            foreach (var position in WorldGrid.GetAllPositions().ToList())
            {
                var tile = WorldGrid.GetTile(position);
                if (tile?.Fluid == null) continue;
                if (tile.Fluid.IsWaterfall) continue;
                if (WorldGrid.HasSolidBelow(position.x, position.y, position.z)) continue;

                if (position.z > 0 && WorldGrid.HasFluid(position.x, position.y, position.z - 1)) continue;

                var landingLevel = WorldGrid.FindLandingLevel(position.x, position.y, position.z - 1);
                if (landingLevel < 0)
                {
                    toRemove.Add(position);
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
        /// Creates transient vertical waterfall columns and transfers volume to landing tiles.
        /// </summary>
        private void CreateWaterfalls()
        {
            var edgesProcessed = new HashSet<(Vector3Int, int)>();
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

                for (var dirIndex = 0; dirIndex < 4; dirIndex++)
                {
                    var nx = position.x + dx[dirIndex];
                    var ny = position.y + dy[dirIndex];
                    var level = position.z;

                    if (!WorldGrid.IsValidPosition(nx, ny)) continue;
                    if (WorldGrid.HasSolidBelow(nx, ny, level)) continue;
                    if (WorldGrid.HasSolidTile(nx, ny, level)) continue;
                    if (WorldGrid.HasFluid(nx, ny, level)) continue;

                    var landingLevel = WorldGrid.FindLandingLevel(nx, ny, level - 1);
                    if (landingLevel < 0) continue;

                    var edgeKey = (position, dirIndex);
                    if (edgesProcessed.Contains(edgeKey)) continue;
                    edgesProcessed.Add(edgeKey);

                    var spreadAmount = Mathf.Min(fill - 1, tile.Fluid.Type.SpreadRate);
                    if (spreadAmount <= 0) continue;

                    for (var columnLevel = level; columnLevel > landingLevel; columnLevel--)
                    {
                        WorldGrid.PlaceFluid(nx, ny, columnLevel, tile.Fluid.Type, fill);
                        var columnTile = WorldGrid.GetTile(nx, ny, columnLevel);
                        if (columnTile?.Fluid != null)
                            columnTile.Fluid.IsWaterfall = true;
                    }

                    WorldGrid.PlaceFluid(nx, ny, landingLevel, tile.Fluid.Type, spreadAmount);
                    var landing = WorldGrid.GetTile(nx, ny, landingLevel);
                    if (landing?.Fluid != null)
                        landing.Fluid.IsWaterfall = false;

                    reductions.TryGetValue(position, out var currentReduction);
                    reductions[position] = currentReduction + spreadAmount;
                }
            }

            foreach (var kvp in reductions)
            {
                var sourceTile = WorldGrid.GetTile(kvp.Key);
                if (sourceTile?.Fluid != null)
                    sourceTile.Fluid.RemoveFillAmount(Mathf.Min(kvp.Value, sourceTile.Fluid.FillAmount - 1));
            }
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

        private void EqualizeBodies()
        {
            foreach (var fluidBody in fluidBodies)
                EqualizeBody(fluidBody);
        }

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

                foreach (var t in levelTiles)
                    t.FillAmount = 0;

                var perTile = levelVolume / levelTiles.Count;
                var remainder = levelVolume % levelTiles.Count;

                foreach (var t in levelTiles)
                    t.FillAmount = perTile;

                for (var i = 0; i < remainder; i++)
                    levelTiles[i].AddFillAmount(1);
            }
        }

        /// <summary>
        /// Pushes volume from taller columns into shorter connected columns.
        /// </summary>
        private void ApplyPressure()
        {
            foreach (var body in fluidBodies)
            {
                if (body.IsSettled) continue;

                var columnHeights = new Dictionary<Vector2Int, int>();
                foreach (var kvp in body.Tiles)
                {
                    var col = new Vector2Int(kvp.Key.x, kvp.Key.y);
                    if (!columnHeights.TryGetValue(col, out var current) || kvp.Key.z > current)
                        columnHeights[col] = kvp.Key.z;
                }

                if (columnHeights.Count <= 1) continue;

                Vector2Int tallestCol = default;
                var maxHeight = int.MinValue;
                foreach (var kvp in columnHeights)
                {
                    if (kvp.Value > maxHeight)
                    {
                        maxHeight = kvp.Value;
                        tallestCol = kvp.Key;
                    }
                }

                Vector2Int shortestCol = default;
                var minHeight = int.MaxValue;
                var foundShortest = false;

                foreach (var kvp in columnHeights)
                {
                    if (kvp.Key == tallestCol) continue;

                    var nextLevel = kvp.Value + 1;
                    if (nextLevel >= WorldGrid.MaxLevels) continue;
                    if (WorldGrid.HasSolidTile(kvp.Key.x, kvp.Key.y, nextLevel)) continue;

                    if (kvp.Value < minHeight)
                    {
                        minHeight = kvp.Value;
                        shortestCol = kvp.Key;
                        foundShortest = true;
                    }
                }

                if (!foundShortest) continue;
                if (maxHeight <= minHeight) continue;

                var tallTopPos = new Vector3Int(tallestCol.x, tallestCol.y, maxHeight);
                var tallTopTile = WorldGrid.GetTile(tallTopPos);
                if (tallTopTile?.Fluid == null) continue;

                var transferAmount = Mathf.Min(tallTopTile.Fluid.FillAmount, body.Type.SpreadRate);
                if (transferAmount <= 0) continue;

                tallTopTile.Fluid.RemoveFillAmount(transferAmount);

                var shortTopTile = WorldGrid.GetTile(shortestCol.x, shortestCol.y, minHeight);
                if (shortTopTile?.Fluid != null && !shortTopTile.Fluid.IsFull)
                {
                    WorldGrid.PlaceFluid(shortestCol.x, shortestCol.y, minHeight, body.Type, transferAmount);
                }
                else
                {
                    var nextLevel = minHeight + 1;
                    if (nextLevel < WorldGrid.MaxLevels)
                        WorldGrid.PlaceFluid(shortestCol.x, shortestCol.y, nextLevel, body.Type, transferAmount);
                }
            }
        }

        private void FindConnectedBodies()
        {
            fluidBodies.Clear();
            nextBodyId = 0;

            foreach (var tile in WorldGrid.GetAllFluidTiles())
                if (tile.Fluid != null)
                    tile.Fluid.BodyId = -1;

            foreach (var position in WorldGrid.GetAllPositions())
            {
                var tile = WorldGrid.GetTile(position);
                if (tile?.Fluid == null) continue;
                if (tile.Fluid.BodyId != -1) continue;

                var body = FloodFillBody(position, tile.Fluid);
                fluidBodies.Add(body);
            }
        }

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
                if (currentFluid.Type != body.Type) continue;

                body.AddTile(currentPos, currentFluid);

                if (currentFluid.IsSettled) hasSettled = true;
                else hasUnsettled = true;

                var neighbours = WorldGrid.GetAllCardinalNeighbours(currentPos);
                foreach (var neighbourPos in neighbours)
                    if (CanFluidConnect(currentPos, neighbourPos))
                        queue.Enqueue(neighbourPos);
            }

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
            if (!Application.isPlaying && WorldGrid != null && WorldGrid.GetAllFluidTiles().Any())
                if (!simulateInEditor)
                    FindConnectedBodies();
        }
#endif
    }
}

