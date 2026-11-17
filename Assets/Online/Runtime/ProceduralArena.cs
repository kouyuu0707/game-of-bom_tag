using Unity.AI.Navigation;
using UnityEngine;

namespace BombTag.Online
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class ProceduralArena : MonoBehaviour
    {
        [SerializeField] private Material wallMaterial;
        private GameObject generatedRoot;
        public GeneratedMap Layout { get; private set; }

        public void Generate(int seed)
        {
            if (Layout != null && Layout.Seed == seed && generatedRoot != null) return;
            Clear();
            // Keep older saved scenes in sync with the new map footprint as well.
            transform.localScale = new Vector3(GeneratedMap.GroundSize / 10f,
                transform.localScale.y, GeneratedMap.GroundSize / 10f);
            Layout = MapGenerator.Generate(seed);
            generatedRoot = new GameObject("Generated Map - " + seed);
            // Preserve world scale even though the ground plane is scaled.
            generatedRoot.transform.SetParent(transform, true);
            for (int z = 0; z < GeneratedMap.Size; z++)
            for (int x = 0; x < GeneratedMap.Size; x++)
            {
                if (!Layout.IsWall(x, z)) continue;
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"Wall {x},{z}";
                wall.transform.SetParent(generatedRoot.transform, false);
                wall.transform.localPosition = new Vector3(GeneratedMap.WorldCoordinate(x), 1.25f, GeneratedMap.WorldCoordinate(z));
                wall.transform.localScale = new Vector3(GeneratedMap.CellSize, 2.5f, GeneratedMap.CellSize);
                wall.GetComponent<Renderer>().sharedMaterial = wallMaterial != null
                    ? wallMaterial : GetComponent<Renderer>().sharedMaterial;
                wall.AddComponent<GeneratedWall>();
            }
            Physics.SyncTransforms();
        }

        public void Clear()
        {
            if (generatedRoot != null)
            {
                // Remove old collision and navigation sources immediately; Destroy is deferred.
                generatedRoot.SetActive(false);
                Destroy(generatedRoot);
            }
            generatedRoot = null;
            Layout = null;
        }

        public static Vector3 Position(MapCell cell, float height) =>
            new Vector3(GeneratedMap.WorldCoordinate(cell.X), height, GeneratedMap.WorldCoordinate(cell.Z));
    }
}
