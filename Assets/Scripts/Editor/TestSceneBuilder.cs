// TestSceneBuilder: TEMPORARY Phase 1 tool. Menu "The Last Torch > Dev > Build Test Scene" creates
// Assets/Scenes/TestScene.unity with a dark floor, some walls and a corridor (to test camera
// collision), the explorer, two oil jars, the managers and a debug overlay. The real Temple scene
// is built by SceneBuilder in Phase 3; this file can be deleted after that.
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static LastTorch.EditorTools.EditorUtil;

namespace LastTorch.EditorTools
{
    public static class TestSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/TestScene.unity";

        [MenuItem("The Last Torch/Dev/Build Test Scene (Phase 1)", priority = 200)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
            Debug.Log("[TestSceneBuilder] Built " + ScenePath + ". Press Play to test.");
        }

        /// <summary>Command-line entry point (batchmode -executeMethod).</summary>
        public static void BuildBatch()
        {
            try { Build(); }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void Build()
        {
            var settings = GameSettingsAsset.GetOrCreate();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Darkness: no sun, flat near-black ambient, black fog
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = settings.ambientColor;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.black;
            RenderSettings.fogDensity = settings.fogDensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;

            var floorMat = MaterialFactory.Lit("Test_Floor", Hex("5A564C"), 0.1f);
            var wallMat = MaterialFactory.Lit("Test_Wall", Hex("4F5D50"), 0.1f);
            var clayMat = MaterialFactory.LitEmissive("Test_Clay", Hex("8A4A24"), Hex("8A4A24") * 0.35f, 0.3f);

            // Level geometry
            var level = new GameObject("TestLevel").transform;
            Block("Floor", level, new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.2f, 40f), floorMat);
            Block("Wall_N", level, new Vector3(0f, 2f, 20.5f), new Vector3(42f, 4f, 1f), wallMat);
            Block("Wall_S", level, new Vector3(0f, 2f, -20.5f), new Vector3(42f, 4f, 1f), wallMat);
            Block("Wall_E", level, new Vector3(20.5f, 2f, 0f), new Vector3(1f, 4f, 40f), wallMat);
            Block("Wall_W", level, new Vector3(-20.5f, 2f, 0f), new Vector3(1f, 4f, 40f), wallMat);
            Block("Corridor_L", level, new Vector3(-2.5f, 2f, 10f), new Vector3(1f, 4f, 12f), wallMat);
            Block("Corridor_R", level, new Vector3(2.5f, 2f, 10f), new Vector3(1f, 4f, 12f), wallMat);
            Block("Room_Block", level, new Vector3(8f, 2f, -8f), new Vector3(4f, 4f, 4f), wallMat);
            Block("Pillar", level, new Vector3(-6f, 2f, -10f), new Vector3(1.2f, 4f, 1.2f), wallMat);
            Block("LowWall", level, new Vector3(-8f, 0.5f, 6f), new Vector3(6f, 1f, 0.5f), wallMat);

            // Player
            var explorerMats = ExplorerBuilder.CreateMaterials();
            var player = ExplorerBuilder.Build(explorerMats, new Vector3(0f, 0f, -2f), Quaternion.identity);

            // Camera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0f, 2.5f, -7f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraShake>();
            var tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.target = player.transform;
            player.GetComponent<PlayerController>().cameraTransform = camGo.transform;

            // Managers
            var managers = new GameObject("GameManager");
            var gm = managers.AddComponent<GameManager>();
            gm.settings = settings;
            gm.playIntro = false;
            managers.AddComponent<ScoreManager>();
            managers.AddComponent<AudioManager>();
            managers.AddComponent<DebugOverlay>();

            // Oil jars
            OilJar(new Vector3(0f, 0f, 12f), clayMat);
            OilJar(new Vector3(-10f, 0f, -4f), clayMat);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
        }

        static void Block(string name, Transform parent, Vector3 position, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.isStatic = true;
        }

        /// <summary>Placeholder jar from primitives (Phase 2 replaces it with the generated OilJar prefab).</summary>
        static void OilJar(Vector3 position, Material clay)
        {
            var root = new GameObject("OilJar");
            root.transform.position = position;

            var visual = Empty("Visual", root.transform, new Vector3(0f, 0.45f, 0f)).transform;
            Part(PrimitiveType.Sphere, "Body", visual, Vector3.zero, new Vector3(0.5f, 0.55f, 0.5f), clay);
            Part(PrimitiveType.Cylinder, "Neck", visual, new Vector3(0f, 0.32f, 0f), new Vector3(0.2f, 0.08f, 0.2f), clay);
            Part(PrimitiveType.Cylinder, "Lip", visual, new Vector3(0f, 0.4f, 0f), new Vector3(0.28f, 0.02f, 0.28f), clay);

            var trigger = root.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.7f;
            trigger.center = new Vector3(0f, 0.5f, 0f);
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var c = root.AddComponent<Collectible>();
            c.type = CollectibleType.Oil;
            c.visual = visual;
            c.spin = false;
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == path)) return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
