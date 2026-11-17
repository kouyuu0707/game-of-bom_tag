using System;
using System.Collections.Generic;

namespace BombTag.Online
{
    public readonly struct MapCell : IEquatable<MapCell>
    {
        public readonly int X;
        public readonly int Z;
        public MapCell(int x, int z) { X = x; Z = z; }
        public bool Equals(MapCell other) => X == other.X && Z == other.Z;
        public override bool Equals(object obj) => obj is MapCell other && Equals(other);
        public override int GetHashCode() => X * 397 ^ Z;
        public override string ToString() => $"({X}, {Z})";
    }

    public sealed class GeneratedMap
    {
        public const int Size = 25;
        public const float CellSize = 3f;
        public const float WorldSize = Size * CellSize;
        public const float GroundSize = WorldSize + 5f;
        public const float MovementLimit = WorldSize / 2f + 0.5f;
        public const int MinimumPassageCells = 2;
        public const float MinimumPassageWidth = CellSize * MinimumPassageCells;
        private readonly bool[] walls;
        public int Seed { get; }
        public IReadOnlyList<MapCell> Players { get; }
        public IReadOnlyList<MapCell> Enemies { get; }
        public IReadOnlyList<MapCell> Pickups { get; }

        internal GeneratedMap(int seed, bool[] walls, MapCell[] players, MapCell[] enemies, MapCell[] pickups)
        {
            Seed = seed;
            this.walls = walls;
            Players = Array.AsReadOnly(players);
            Enemies = Array.AsReadOnly(enemies);
            Pickups = Array.AsReadOnly(pickups);
        }

        public bool IsWall(int x, int z) => x < 0 || z < 0 || x >= Size || z >= Size || walls[z * Size + x];
        public static float WorldCoordinate(int cell) => (cell - Size / 2) * CellSize;
    }

    public static class MapGenerator
    {
        private sealed class RandomState
        {
            private uint state;
            public RandomState(int seed)
            {
                state = unchecked((uint)seed) ^ 0x9E3779B9u;
                if (state == 0) state = 0xA341316Cu;
            }
            public int Next(int max)
            {
                // Explicit integer algorithm: identical results in Editor and Windows builds.
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return (int)(state % (uint)max);
            }
            public void Shuffle<T>(List<T> list)
            {
                for (int i = list.Count - 1; i > 0; i--)
                {
                    int j = Next(i + 1);
                    T value = list[i]; list[i] = list[j]; list[j] = value;
                }
            }
        }

        public static GeneratedMap Generate(int seed)
        {
            int size = GeneratedMap.Size;
            int center = size / 2;
            var random = new RandomState(seed);
            var walls = new bool[size * size];
            var candidates = new List<MapCell>();
            for (int z = 0; z < size; z++)
            for (int x = 0; x < size; x++)
            {
                walls[z * size + x] = x == 0 || z == 0 || x == size - 1 || z == size - 1;
                // Keep a two-cell-wide escape circuit around the entire arena.
                if (x > 2 && z > 2 && x < size - 3 && z < size - 3 &&
                    (Math.Abs(x - center) > 2 || Math.Abs(z - center) > 2))
                    candidates.Add(new MapCell(x, z));
            }
            random.Shuffle(candidates);
            int openCount = (size - 2) * (size - 2);
            int placed = 0;
            foreach (MapCell cell in candidates)
            {
                int index = cell.Z * size + cell.X;
                walls[index] = true;
                if (!HasWideRoutes(walls) || !HasAlternativeRoutes(walls, openCount - 1))
                    walls[index] = false;
                else
                {
                    openCount--;
                    // Preserve approximately the previous obstacle density as the arena grows.
                    if (++placed == (size - 2) * (size - 2) * 40 / 169) break;
                }
            }

            var players = new[] { new MapCell(center - 1, center), new MapCell(center + 1, center) };
            var used = new HashSet<MapCell>(players);
            var floors = new List<MapCell>();
            int[] fromStart = Distances(walls);
            for (int z = 1; z < size - 1; z++)
            for (int x = 1; x < size - 1; x++)
                if (!walls[z * size + x] && fromStart[z * size + x] >= 6)
                    floors.Add(new MapCell(x, z));
            random.Shuffle(floors);
            var enemies = new List<MapCell>();
            foreach (MapCell cell in floors)
            {
                if (enemies.Count > 0 && Math.Abs(cell.X - enemies[0].X) + Math.Abs(cell.Z - enemies[0].Z) < 4)
                    continue;
                enemies.Add(cell);
                used.Add(cell);
                if (enemies.Count == 2) break;
            }
            if (enemies.Count != 2)
                throw new InvalidOperationException("Map could not place two enemies safely.");

            floors.Clear();
            for (int z = 1; z < size - 1; z++)
            for (int x = 1; x < size - 1; x++)
            {
                var cell = new MapCell(x, z);
                if (!walls[z * size + x] && !used.Contains(cell) && fromStart[z * size + x] >= 3)
                    floors.Add(cell);
            }
            random.Shuffle(floors);
            return new GeneratedMap(seed, walls, players, enemies.ToArray(), floors.GetRange(0, 8).ToArray());
        }

        private static bool HasWideRoutes(bool[] walls)
        {
            int size = GeneratedMap.Size;
            for (int z = 1; z < size - 1; z++)
            for (int x = 1; x < size - 1; x++)
            {
                if (walls[z * size + x]) continue;
                bool hasSpace = false;
                // Every floor cell must belong to a fully open 2x2 area.
                for (int dz = -1; dz <= 0; dz++)
                for (int dx = -1; dx <= 0; dx++)
                {
                    int corner = (z + dz) * size + x + dx;
                    if (!walls[corner] && !walls[corner + 1] &&
                        !walls[corner + size] && !walls[corner + size + 1]) hasSpace = true;
                }
                if (!hasSpace) return false;
            }
            return true;
        }

        private static bool HasAlternativeRoutes(bool[] walls, int expectedOpenCount)
        {
            // Tarjan's articulation-point check: no single floor cell may be the
            // only link between two areas. This rules out dead ends and bottlenecks.
            int size = GeneratedMap.Size;
            var order = new int[walls.Length];
            var low = new int[walls.Length];
            for (int i = 0; i < order.Length; i++) order[i] = -1;
            int visited = 0;
            int[] offsets = { -1, 1, -size, size };
            bool Visit(int current, int parent)
            {
                order[current] = low[current] = visited++;
                int children = 0;
                foreach (int offset in offsets)
                {
                    int next = current + offset;
                    if (next < 0 || next >= walls.Length || walls[next]) continue;
                    if (order[next] < 0)
                    {
                        children++;
                        if (!Visit(next, current)) return false;
                        low[current] = Math.Min(low[current], low[next]);
                        if (parent >= 0 && low[next] >= order[current]) return false;
                    }
                    else if (next != parent) low[current] = Math.Min(low[current], order[next]);
                }
                return parent >= 0 || children <= 1;
            }
            return Visit((size / 2) * size + size / 2, -1) && visited == expectedOpenCount;
        }

        private static int[] Distances(bool[] walls)
        {
            int size = GeneratedMap.Size;
            var distances = new int[size * size];
            for (int i = 0; i < distances.Length; i++) distances[i] = -1;
            var queue = new Queue<int>();
            int start = (size / 2) * size + size / 2;
            distances[start] = 0;
            queue.Enqueue(start);
            int[] offsets = { -1, 1, -size, size };
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int offset in offsets)
                {
                    int next = current + offset;
                    // The closed outer border means no open cell can wrap to the next row.
                    if (next < 0 || next >= walls.Length || walls[next] || distances[next] >= 0) continue;
                    distances[next] = distances[current] + 1;
                    queue.Enqueue(next);
                }
            }
            return distances;
        }
    }
}
