using BombTag.Online;
using NUnit.Framework;
using UnityEngine;

public class ArenaVisibilityTests
{
    [Test]
    public void WallStopsDamageAndClipsExplosionDisplay()
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            wall.AddComponent<GeneratedWall>();
            Physics.SyncTransforms();
            Vector3 from = new Vector3(-2, 0, 0), to = new Vector3(2, 0, 0);
            Assert.That(ArenaVisibility.IsClear(from, to), Is.False);
            Assert.That(ArenaVisibility.ClipAtWall(from, to).x, Is.LessThan(-0.49f));
        }
        finally { Object.DestroyImmediate(wall); }
    }

    [Test]
    public void ActorsDoNotShieldOthersFromTheBlast()
    {
        GameObject actor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            Physics.SyncTransforms();
            Assert.That(ArenaVisibility.IsClear(new Vector3(-2, 0, 0), new Vector3(2, 0, 0)), Is.True);
        }
        finally { Object.DestroyImmediate(actor); }
    }

    [Test]
    public void InactiveOldWallDoesNotBlockTheNewMap()
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        try
        {
            wall.AddComponent<GeneratedWall>();
            wall.SetActive(false);
            Physics.SyncTransforms();
            Assert.That(ArenaVisibility.IsClear(new Vector3(-2, 0, 0), new Vector3(2, 0, 0)), Is.True);
        }
        finally { Object.DestroyImmediate(wall); }
    }
}
