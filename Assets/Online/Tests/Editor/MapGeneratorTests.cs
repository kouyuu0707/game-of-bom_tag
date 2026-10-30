using System.Collections.Generic;
using System.Linq;
using System.Text;
using BombTag.Online;
using NUnit.Framework;

public class MapGeneratorTests
{
    [Test]
    public void SameSeedRecreatesWallsAndEverySpawn()
    {
        Assert.That(Fingerprint(MapGenerator.Generate(12345)), Is.EqualTo(Fingerprint(MapGenerator.Generate(12345))));
    }

    [Test]
    public void DifferentSeedsGiveDifferentMaps()
    {
        var layouts = new HashSet<string>();
        for (int seed = 1; seed <= 32; seed++) layouts.Add(Fingerprint(MapGenerator.Generate(seed)));
        Assert.That(layouts.Count, Is.GreaterThanOrEqualTo(30));
    }

    [Test]
    public void EveryOpenCellIsReachableAcrossOneThousandSeeds()
    {
        for (int seed = 1; seed <= 1000; seed++)
        {
            GeneratedMap map = MapGenerator.Generate(seed);
            var visited = new HashSet<MapCell>();
            var queue = new Queue<MapCell>();
            visited.Add(map.Players[0]); queue.Enqueue(map.Players[0]);
            while (queue.Count > 0)
            {
                MapCell current = queue.Dequeue();
                foreach (MapCell neighbor in new[] { new MapCell(current.X - 1, current.Z),
                    new MapCell(current.X + 1, current.Z), new MapCell(current.X, current.Z - 1), new MapCell(current.X, current.Z + 1) })
                    if (!map.IsWall(neighbor.X, neighbor.Z) && visited.Add(neighbor)) queue.Enqueue(neighbor);
            }
            for (int z = 0; z < GeneratedMap.Size; z++)
            for (int x = 0; x < GeneratedMap.Size; x++)
                if (!map.IsWall(x, z)) Assert.That(visited.Contains(new MapCell(x, z)), Is.True, $"seed {seed}: {x},{z}");
        }
    }

    [Test]
    public void BoundaryIsClosedAndStartingPlazaIsOpen()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            GeneratedMap map = MapGenerator.Generate(seed);
            for (int i = 0; i < GeneratedMap.Size; i++)
            {
                Assert.That(map.IsWall(0, i) && map.IsWall(i, 0) &&
                    map.IsWall(GeneratedMap.Size - 1, i) && map.IsWall(i, GeneratedMap.Size - 1), Is.True);
            }
            for (int z = 5; z <= 9; z++)
            for (int x = 5; x <= 9; x++) Assert.That(map.IsWall(x, z), Is.False);
        }
    }

    [Test]
    public void PlayersEnemiesAndSuppliesHaveDistinctOpenSpawns()
    {
        for (int seed = 0; seed < 500; seed++)
        {
            GeneratedMap map = MapGenerator.Generate(seed);
            Assert.That(map.Players.Count, Is.EqualTo(2));
            Assert.That(map.Enemies.Count, Is.EqualTo(2));
            Assert.That(map.Pickups.Count, Is.EqualTo(8));
            var all = map.Players.Concat(map.Enemies).Concat(map.Pickups).ToArray();
            Assert.That(all.Distinct().Count(), Is.EqualTo(12));
            foreach (MapCell cell in all) Assert.That(map.IsWall(cell.X, cell.Z), Is.False);
            foreach (MapCell enemy in map.Enemies)
                Assert.That(System.Math.Abs(enemy.X - 7) > 2 || System.Math.Abs(enemy.Z - 7) > 2, Is.True);
        }
    }

    [Test]
    public void EdgeCaseSeedsAreSupported()
    {
        foreach (int seed in new[] { 0, -1, int.MinValue, int.MaxValue, unchecked((int)0x9E3779B9u) })
        {
            GeneratedMap map = MapGenerator.Generate(seed);
            Assert.That(map.Seed, Is.EqualTo(seed));
            Assert.That(map.Enemies.Count, Is.EqualTo(2));
            Assert.That(map.Pickups.Count, Is.EqualTo(8));
        }
    }

    [Test]
    public void WallsFitInsideTheExistingGroundAndMovementBounds()
    {
        Assert.That(GeneratedMap.WorldCoordinate(7), Is.EqualTo(0));
        Assert.That(GeneratedMap.WorldCoordinate(0) - GeneratedMap.CellSize / 2, Is.GreaterThan(-23));
        Assert.That(GeneratedMap.WorldCoordinate(14) + GeneratedMap.CellSize / 2, Is.LessThan(23));
    }

    private static string Fingerprint(GeneratedMap map)
    {
        var text = new StringBuilder();
        for (int z = 0; z < GeneratedMap.Size; z++)
        for (int x = 0; x < GeneratedMap.Size; x++) text.Append(map.IsWall(x, z) ? '#' : '.');
        foreach (MapCell cell in map.Players.Concat(map.Enemies).Concat(map.Pickups)) text.Append(cell);
        return text.ToString();
    }
}
