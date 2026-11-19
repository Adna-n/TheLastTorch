// TestSceneBuilder: TEMPORARY test tool (removed once the Temple scene exists in Phase 3).
// Builds Assets/Scenes/TestScene.unity: a dark stone room with a corridor (camera collision test),
// the Player prefab, two oil jars, managers, post-processing, the debug overlay and a "showcase"
// of every generated prefab so you can check how they look. Needs "1. Generate Assets" first.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static LastTorch.EditorTools.SceneBuilder;

namespace LastTorch.EditorTools
{
    public static class TestSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/TestScene.unity";

        [MenuItem("The Last Torch/Dev/Build Test Scene", priority = 200)]
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
            if (PrefabFactory.Load("Player") == null)
                throw new Exception("Generated assets missing. Run 'The Last Torch > 1. Generate Assets' first.");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupDarkness(settings.ambientColor, settings.fogDensity);

            var wallMat = MaterialFactory.Load("Stone_Wall");
            var floorMat = MaterialFactory.Load("Stone_Floor");

            // Level geometry (box meshes with world-scaled UVs so the stone texture tiles evenly)
            var level = new GameObject("TestLevel").transform;
            Block("Floor", level, new Vector3(0f, -0.1f, 0f), new Vector3(40f, 0.2f, 40f), floorMat);
            Block("Wall_N", level, new Vector3(0f, 2f, 20.5f), new Vector3(42f, 4f, 1f), wallMat);
            Block("Wall_S", level, new Vector3(0f, 2f, -20.5f), new Vector3(42f, 4f, 1f), wallMat);
            Block("Wall_E", level, new Vector3(20.5f, 2f, 0f), new Vector3(1f, 4f, 40f), wallMat);
            Block("Wall_W", level, new Vector3(-20.5f, 2f, 0f), new Vector3(1f, 4f, 40f), wallMat);
            Block("Corridor_L", level, new Vector3(-2.5f, 2f, 10f), new Vector3(1f, 4f, 12f), wallMat);
            Block("Corridor_R", level, new Vector3(2.5f, 2f, 10f), new Vector3(1f, 4f, 12f), wallMat);
            Block("Room_Block", level, new Vector3(8f, 2f, -4f), new Vector3(4f, 4f, 4f), wallMat);
            Block("LowWall", level, new Vector3(-8f, 0.5f, 6f), new Vector3(6f, 1f, 0.5f), wallMat);

            // Player + camera
            var player = Instantiate("Player", new Vector3(0f, 0f, -2f), Quaternion.identity);
            var cam = CreateCamera(new Vector3(0f, 2.5f, -7f), Quaternion.identity, 60f);
            cam.gameObject.AddComponent<CameraShake>();
            cam.gameObject.AddComponent<ThirdPersonCamera>().target = player.transform;
            player.GetComponent<PlayerController>().cameraTransform = cam.transform;

            // Managers
            var managers = new GameObject("GameManager");
            var gm = managers.AddComponent<GameManager>();
            gm.settings = settings;
            gm.playIntro = false;
            managers.AddComponent<ScoreManager>();
            CreateAudioManager(managers.transform);
            managers.AddComponent<DebugOverlay>();

            PostFXBuilder.CreateVolume("Global Volume", PostFXBuilder.LoadGlobal(), 1f, 0f);
            PostFXBuilder.CreateVolume("Collapse Volume", PostFXBuilder.LoadCollapse(), 0f, 1f);

            // Oil jars to test refuelling
            Instantiate("OilJar", new Vector3(0f, 0f, 12f), Quaternion.identity);
            Instantiate("OilJar", new Vector3(-10f, 0f, -4f), Quaternion.identity);

            BuildShowcase();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Places one of every prefab around the room for a visual check.</summary>
        static void BuildShowcase()
        {
            var root = new GameObject("PrefabShowcase").transform;
            const float lift = 0.005f; // floor-tile prefabs sit just above the test floor (no z-fighting)

            // Small items in a row along the south side
            string[] items = { "Gem", "Key_Sun", "Key_Moon", "Key_Serpent", "OilJar", "Debris", "Rubble", "Dart", "Pillar", "HeartAltar" };
            for (int i = 0; i < items.Length; i++)
                Instantiate(items[i], new Vector3(-15f + i * 3.2f, 0f, -12f), Quaternion.identity, root);
            Instantiate("DartLauncher", new Vector3(17f, 1.2f, -12f), Quaternion.Euler(0f, -90f, 0f), root);

            // Doors against the south wall, their front (sockets) facing north toward the player
            Quaternion faceNorth = Quaternion.Euler(0f, 180f, 0f);
            Instantiate("KeyDoor", new Vector3(-14f, 0f, -17.5f), faceNorth, root);
            Instantiate("SanctumDoor", new Vector3(-8f, 0f, -17.5f), faceNorth, root);
            Instantiate("EntranceDoor", new Vector3(-2f, 0f, -17.5f), faceNorth, root);
            // Exit gate on the east side: front faces west, the daylight "outside" extends east
            Instantiate("ExitGate", new Vector3(13f, 0f, 8f), Quaternion.Euler(0f, 90f, 0f), root);

            // Trap tiles and the boulder in the west area
            Instantiate("SpikeTile", new Vector3(-14f, lift, 0f), Quaternion.identity, root);
            Instantiate("DartPlate", new Vector3(-14f, lift, 6f), Quaternion.identity, root);
            Instantiate("Boulder", new Vector3(-14f, GameSettingsAsset.GetOrCreate().cellSize * 0.38f, 13f), Quaternion.identity, root);

            // Wall decoration on the corridor's inner left wall (faces +X)
            Quaternion faceEast = Quaternion.Euler(0f, -90f, 0f);
            Instantiate("GlyphPanel", new Vector3(-1.99f, 2f, 8f), faceEast, root);
            Instantiate("Vine", new Vector3(-1.98f, 4f, 11.5f), faceEast, root);
            Instantiate("Wall", new Vector3(8f, 0f, 14f), Quaternion.identity, root);
            Instantiate("Floor", new Vector3(8f, lift, 8f), Quaternion.identity, root);
        }

        static void Block(string name, Transform parent, Vector3 position, Vector3 size, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.AddComponent<MeshFilter>().sharedMesh = MeshGenerator.Box("Test_" + name, size, Vector3.zero);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            go.isStatic = true;
        }
    }
}
