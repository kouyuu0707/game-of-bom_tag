using System.IO;
using System.Linq;
using BombTag.Online;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BombTag.OnlineEditor
{
    public static class OnlineSceneBuilder
    {
        public const string ScenePath = "Assets/Online/Generated/OnlineTest.unity";
        private const string PrefabPath = "Assets/Online/Generated/OnlinePlayer.prefab";

        [MenuItem("Tools/Bomb Tag/Create or Open Online Test")]
        public static void CreateOrOpen()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath);
                return;
            }
            Directory.CreateDirectory("Assets/Online/Generated");
            AssetDatabase.Refresh();

            Material playerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerMaterial.mat");
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                avatar.name = "OnlinePlayer";
                Object.DestroyImmediate(avatar.GetComponent<CapsuleCollider>());
                avatar.AddComponent<CharacterController>();
                avatar.AddComponent<NetworkObject>();
                avatar.AddComponent<NetworkTransform>();
                avatar.AddComponent<OnlinePlayer>();
                avatar.GetComponent<Renderer>().sharedMaterial = playerMaterial;
                prefab = PrefabUtility.SaveAsPrefabAsset(avatar, PrefabPath);
                Object.DestroyImmediate(avatar);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5, 1, 5);
            string materialPath = "Assets/Online/Generated/Ground.mat";
            Material groundMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (groundMaterial == null)
            {
                groundMaterial = new Material(playerMaterial);
                groundMaterial.name = "Online Ground";
                groundMaterial.SetColor("_BaseColor", new Color(0.2f, 0.3f, 0.22f));
                AssetDatabase.CreateAsset(groundMaterial, materialPath);
            }
            ground.GetComponent<Renderer>().sharedMaterial = groundMaterial;

            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 12, -10);
            cameraObject.transform.LookAt(Vector3.zero);
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.22f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            GameObject lightObject = new GameObject("Sun", typeof(Light));
            lightObject.GetComponent<Light>().type = LightType.Directional;
            lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = Color.gray;

            GameObject connection = new GameObject("Online Connection");
            OnlineSessionController controller = connection.AddComponent<OnlineSessionController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("playerPrefab").objectReferenceValue = prefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!EditorBuildSettings.scenes.Any(item => item.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Online test scene created. Press Play; use Local Host in one window and Local Join in the other.");
        }

        [MenuItem("Tools/Bomb Tag/Build Windows Connection Test")]
        public static void BuildWindowsTest()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Create the online scene first",
                    "Run Tools / Bomb Tag / Create or Open Online Test first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            string folder = EditorUtility.SaveFolderPanel("Choose a build output folder", "", "BombTagOnline");
            if (string.IsNullOrEmpty(folder))
                return;
            if (Path.GetFullPath(folder).StartsWith(Path.GetFullPath(Application.dataPath),
                System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Choose another folder", "Build outside the Assets folder.", "OK");
                return;
            }
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = Path.Combine(folder, "BombTagOnline.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log("Connection test build created: " + Path.Combine(folder, "BombTagOnline.exe"));
            else
                Debug.LogError("Connection test build failed. Check the first Console error.");
        }
    }
}
