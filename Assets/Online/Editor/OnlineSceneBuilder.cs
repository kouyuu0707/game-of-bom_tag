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
        private const string BombPrefabPath = "Assets/Online/Generated/OnlineBomb.prefab";

        [MenuItem("Tools/Bomb Tag/Create or Open Online Test")]
        public static void CreateOrOpen() => PrepareScene();

        private static bool PrepareScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;
            Directory.CreateDirectory("Assets/Online/Generated");
            AssetDatabase.Refresh();
            Material playerMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/PlayerMaterial.mat");
            GameObject bombPrefab = EnsureBombPrefab(playerMaterial);
            GameObject prefab = EnsurePlayerPrefab(playerMaterial, bombPrefab);

            Scene scene;
            OnlineSessionController controller;
            if (File.Exists(ScenePath))
            {
                // Update only the new references. Keep the user's scene layout and existing assets.
                scene = EditorSceneManager.OpenScene(ScenePath);
                controller = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<OnlineSessionController>(true)).FirstOrDefault();
                if (controller == null)
                {
                    Debug.LogError("OnlineTest has no OnlineSessionController. Restore that component before building.");
                    return false;
                }
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
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
                controller = new GameObject("Online Connection").AddComponent<OnlineSessionController>();
            }

            SerializedObject serialized = new SerializedObject(controller);
            SerializedProperty playerReference = serialized.FindProperty("playerPrefab");
            if (playerReference.objectReferenceValue == null)
                playerReference.objectReferenceValue = prefab;
            else if (playerReference.objectReferenceValue != prefab)
            {
                // Preserve a custom player prefab, upgrading its inventory reference too.
                string customPath = AssetDatabase.GetAssetPath(playerReference.objectReferenceValue);
                if (string.IsNullOrEmpty(customPath))
                {
                    Debug.LogError("Player Prefab must reference a saved prefab asset.");
                    return false;
                }
                UpgradeInventory(customPath, bombPrefab);
            }
            serialized.FindProperty("bombPrefab").objectReferenceValue = bombPrefab;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!EditorBuildSettings.scenes.Any(item => item.path == ScenePath))
                EditorBuildSettings.scenes = EditorBuildSettings.scenes
                    .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("Online test ready. Local Host / Local Join to connect; B places bombs, E detonates your bombs.");
            return true;
        }

        private static GameObject EnsureBombPrefab(Material material)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BombPrefabPath);
            if (prefab != null)
                return prefab;
            GameObject bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            try
            {
                bomb.name = "OnlineBomb";
                bomb.transform.localScale = Vector3.one * 0.5f;
                Object.DestroyImmediate(bomb.GetComponent<SphereCollider>());
                bomb.AddComponent<NetworkObject>();
                bomb.AddComponent<OnlineBomb>();
                bomb.GetComponent<Renderer>().sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(bomb, BombPrefabPath);
            }
            finally { Object.DestroyImmediate(bomb); }
        }

        private static GameObject EnsurePlayerPrefab(Material material, GameObject bombPrefab)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                GameObject avatar = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                try
                {
                    avatar.name = "OnlinePlayer";
                    Object.DestroyImmediate(avatar.GetComponent<CapsuleCollider>());
                    avatar.AddComponent<CharacterController>();
                    avatar.AddComponent<NetworkObject>();
                    avatar.AddComponent<NetworkTransform>();
                    avatar.AddComponent<OnlinePlayer>();
                    avatar.GetComponent<Renderer>().sharedMaterial = material;
                    PrefabUtility.SaveAsPrefabAsset(avatar, PrefabPath);
                }
                finally { Object.DestroyImmediate(avatar); }
            }
            UpgradeInventory(PrefabPath, bombPrefab);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        private static void UpgradeInventory(string path, GameObject bombPrefab)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                OnlineBombInventory inventory = root.GetComponent<OnlineBombInventory>();
                bool added = inventory == null;
                if (added)
                    inventory = root.AddComponent<OnlineBombInventory>();
                SerializedObject serialized = new SerializedObject(inventory);
                serialized.FindProperty("bombPrefab").objectReferenceValue = bombPrefab.GetComponent<OnlineBomb>();
                bool changed = serialized.ApplyModifiedPropertiesWithoutUndo();
                if (added || changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [MenuItem("Tools/Bomb Tag/Build Windows Connection Test")]
        public static void BuildWindowsTest()
        {
            // Also upgrades existing generated assets, so an older local scene is not built by mistake.
            if (!PrepareScene())
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

        [MenuItem("Tools/Bomb Tag/Create or Open Online Test", true)]
        [MenuItem("Tools/Bomb Tag/Build Windows Connection Test", true)]
        private static bool CanPrepare() => !EditorApplication.isPlayingOrWillChangePlaymode;
    }
}
