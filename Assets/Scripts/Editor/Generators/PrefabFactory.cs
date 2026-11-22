// PrefabFactory: assembles every game object from the generated meshes and materials and saves
// them as prefabs in Assets/Generated/Prefabs (walls, floor, collectibles, doors, Heart altar,
// traps, boulder, decorations, effects and the Player) with their behaviour scripts wired up
// (trap scripts arrive in Phase 4). Re-running overwrites the prefabs in place.
// Convention for doors: the slab spans local X, the player approaches from local -Z ("front").
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastTorch.EditorTools
{
    public static class PrefabFactory
    {
        public const string Folder = "Assets/Generated/Prefabs";

        public static string PathOf(string name) => $"{Folder}/{AssetCategories.Prefab(name)}/{name}.prefab";
        public static GameObject Load(string name) => AssetDatabase.LoadAssetAtPath<GameObject>(PathOf(name));

        static Material M(string name)
        {
            var m = MaterialFactory.Load(name);
            if (m == null) Debug.LogWarning("[PrefabFactory] Missing material " + name);
            return m;
        }

        static Mesh Me(string name)
        {
            var m = MeshGenerator.Load(name);
            if (m == null) Debug.LogWarning("[PrefabFactory] Missing mesh " + name);
            return m;
        }

        /// <summary>A child with a mesh + material and no collider.</summary>
        static GameObject Visual(string name, Transform parent, string mesh, string material, Vector3 pos,
            Vector3? scale = null, Vector3 euler = default, bool shadows = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale ?? Vector3.one;
            go.AddComponent<MeshFilter>().sharedMesh = Me(mesh);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = M(material);
            r.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            return go;
        }

        static GameObject Save(GameObject go, string name)
        {
            EditorUtil.EnsureFolder($"{Folder}/{AssetCategories.Prefab(name)}");
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PathOf(name));
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void MakeTrigger(GameObject go, float radius, Vector3 center)
        {
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = radius;
            col.center = center;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        static Light AddLight(Transform parent, Vector3 pos, Color color, float range, float intensity, bool shadows)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = intensity;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            return l;
        }

        public static void GenerateAll()
        {
            var s = GameSettingsAsset.GetOrCreate();
            float c = s.cellSize, h = s.wallHeight;

            // ---------------- level blocks
            {
                var go = new GameObject("Wall");
                go.AddComponent<MeshFilter>().sharedMesh = Me("WallBlock");
                go.AddComponent<MeshRenderer>().sharedMaterial = M("Stone_Wall");
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, h * 0.5f, 0f);
                col.size = new Vector3(c, h, c);
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                Save(go, "Wall");
            }
            {
                var go = new GameObject("Floor");
                go.AddComponent<MeshFilter>().sharedMesh = Me("FloorTile");
                go.AddComponent<MeshRenderer>().sharedMaterial = M("Stone_Floor");
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.1f, 0f);
                col.size = new Vector3(c, 0.2f, c);
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
                Save(go, "Floor");
            }

            // ---------------- one-shot effects (used by collectibles, doors and debris)
            var pickupBurst = BurstPrefab("PickupBurst", "Particle_Ember", 24, 0.4f, 0.8f, 1.5f, 3.5f, 0.06f, 0.14f, 0.4f, false);
            var dustBurst = BurstPrefab("DustBurst", "Particle_Dust", 30, 1.0f, 1.8f, 0.4f, 2.2f, 0.5f, 1.1f, -0.03f, true);
            GameObject warningCircle;
            {
                var go = new GameObject("WarningCircle");
                Visual("Circle", go.transform, "Quad", "Particle_Warning", Vector3.zero, Vector3.one, new Vector3(90f, 0f, 0f), false);
                warningCircle = Save(go, "WarningCircle");
            }

            // ---------------- collectibles
            {
                var go = new GameObject("OilJar");
                var visual = new GameObject("Visual").transform;
                visual.SetParent(go.transform, false);
                Visual("Jar", visual, "Jar", "Clay", Vector3.zero, Vector3.one * 0.9f);
                Visual("Cork", visual, "Cylinder_12", "Wood", new Vector3(0f, 0.7f, 0f), new Vector3(0.2f, 0.08f, 0.2f));
                MakeTrigger(go, 0.7f, new Vector3(0f, 0.5f, 0f));
                var col = go.AddComponent<Collectible>();
                col.type = CollectibleType.Oil;
                col.visual = visual;
                col.spin = false;
                col.burstPrefab = pickupBurst;
                col.burstColor = new Color(1f, 0.6f, 0.2f);
                Save(go, "OilJar");
            }
            {
                var go = new GameObject("Gem");
                var visual = new GameObject("Visual").transform;
                visual.SetParent(go.transform, false);
                visual.localPosition = new Vector3(0f, 0.8f, 0f);
                Visual("Crystal", visual, "Gem_Octahedron", "Gem", Vector3.zero, Vector3.one * 0.5f, default, false);
                MakeTrigger(go, 0.6f, new Vector3(0f, 0.8f, 0f));
                var col = go.AddComponent<Collectible>();
                col.type = CollectibleType.Gem;
                col.visual = visual;
                col.burstPrefab = pickupBurst;
                col.burstColor = new Color(0.5f, 1f, 0.95f);
                Save(go, "Gem");
            }
            KeyPrefab("Key_Sun", KeyType.Sun, new Color(1f, 0.6f, 0.24f), pickupBurst);
            KeyPrefab("Key_Moon", KeyType.Moon, new Color(0.75f, 0.85f, 1f), pickupBurst);
            KeyPrefab("Key_Serpent", KeyType.Serpent, new Color(0.36f, 1f, 0.48f), pickupBurst);

            // ---------------- doors
            {
                var go = new GameObject("KeyDoor");
                var slab = DoorSlab(go.transform, "Stone_Dark");
                Visual("Glyph_Front", slab, "Quad", "Key_Sun", new Vector3(0f, h * 0.6f, -0.41f), Vector3.one * 1.4f);
                Visual("Glyph_Back", slab, "Quad", "Key_Sun", new Vector3(0f, h * 0.6f, 0.41f), Vector3.one * 1.4f, new Vector3(0f, 180f, 0f));
                var door = go.AddComponent<KeyDoor>();
                door.slab = slab;
                door.requiredKey = KeyType.Sun;
                Save(go, "KeyDoor");
            }
            {
                var go = new GameObject("SanctumDoor");
                var slab = DoorSlab(go.transform, "Stone_Dark");
                string[] keys = { "Sun", "Moon", "Serpent" };
                var sockets = new Renderer[3];
                for (int i = 0; i < 3; i++)
                {
                    float x = (i - 1) * 1.0f;
                    sockets[i] = Visual("Socket_" + keys[i], slab, "Cylinder_12", "Socket_Off", new Vector3(x, 1.6f, -0.42f), new Vector3(0.45f, 0.05f, 0.45f), new Vector3(90f, 0f, 0f)).GetComponent<Renderer>();
                    Visual("SocketGlyph_" + keys[i], slab, "Quad", "Key_" + keys[i], new Vector3(x, 2.4f, -0.41f), Vector3.one * 0.6f);
                }
                Visual("Glyph_Back", slab, "Quad", "Glyph_Wall_1", new Vector3(0f, h * 0.6f, 0.41f), Vector3.one * 1.6f, new Vector3(0f, 180f, 0f));
                var door = go.AddComponent<SanctumDoor>();
                door.slab = slab;
                door.sockets = sockets;
                door.socketOnMaterial = M("Socket_On");
                Save(go, "SanctumDoor");
            }
            {
                // Entrance: starts open with dim daylight outside (local +Z), slams shut after the intro
                var go = new GameObject("EntranceDoor");
                var slab = DoorSlab(go.transform, "Stone_Dark");
                Visual("Glyph_Front", slab, "Quad", "Glyph_Wall_2", new Vector3(0f, h * 0.55f, -0.41f), Vector3.one * 1.6f);
                Visual("Glyph_Back", slab, "Quad", "Glyph_Wall_2", new Vector3(0f, h * 0.55f, 0.41f), Vector3.one * 1.6f, new Vector3(0f, 180f, 0f));
                var outside = Outside(go.transform, c, h, false);
                var light = AddLight(outside, new Vector3(0f, h * 0.6f, c * 0.9f), new Color(1f, 0.9f, 0.72f), 10f, 1.5f, true);
                light.name = "OutsideLight";
                // Invisible wall at the doorway's outer edge: the player can't leave before the slam
                Blocker(outside, new Vector3(0f, h * 0.5f, c * 0.5f), new Vector3(c, h, 0.2f));
                var door = go.AddComponent<EntranceDoor>();
                door.slab = slab;
                door.outsideLight = light;
                door.dustBurstPrefab = dustBurst;
                Save(go, "EntranceDoor");
            }
            {
                // Exit gate + the "outside" behind it (local +Z): sandy floor, side walls, glowing daylight, win trigger
                var go = new GameObject("ExitGate");
                var slab = DoorSlab(go.transform, "Stone_Dark");
                Visual("Glyph_Front", slab, "Quad", "Glyph_Wall_3", new Vector3(0f, h * 0.55f, -0.41f), Vector3.one * 1.6f);
                var outside = Outside(go.transform, c, h, true);
                var light = AddLight(outside, new Vector3(0f, h * 0.6f, c * 0.9f), new Color(1f, 0.92f, 0.75f), 14f, 4f, true);
                light.name = "DaylightLight";
                Blocker(outside, new Vector3(0f, h * 0.5f, c * 1.6f), new Vector3(c, h, 0.2f));

                var trigger = new GameObject("WinTrigger");
                trigger.transform.SetParent(go.transform, false);
                var box = trigger.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = new Vector3(0f, h * 0.5f, c * 0.9f);
                box.size = new Vector3(c, h, c * 0.5f);
                var rb = go.AddComponent<Rigidbody>(); // so the child trigger reports to ExitGate
                rb.isKinematic = true;
                rb.useGravity = false;

                var gate = go.AddComponent<ExitGate>();
                gate.slab = slab;
                gate.daylight = light;
                Save(go, "ExitGate");
            }

            // ---------------- Heart of Eternity on its altar
            {
                var go = new GameObject("HeartAltar");
                Visual("Altar", go.transform, "Altar", "Stone_Dark", Vector3.zero);
                var box = go.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0.59f, 0f);
                box.size = new Vector3(1.8f, 1.18f, 1.8f);

                var heart = new GameObject("Heart");
                heart.transform.SetParent(go.transform, false);
                heart.transform.localPosition = new Vector3(0f, 1.9f, 0f);
                var visual = new GameObject("Visual").transform;
                visual.SetParent(heart.transform, false);
                Visual("Crystal", visual, "Heart_Crystal", "Heart", Vector3.zero, Vector3.one * 0.6f, default, false);
                var glow = AddLight(heart.transform, Vector3.zero, EditorUtil.Hex("2EE6D6"), 8f, 3f, false);
                var hum = heart.AddComponent<AudioSource>();
                hum.clip = AudioGenerator.Load("heart_hum");
                hum.loop = true;
                hum.playOnAwake = true;
                hum.spatialBlend = 1f;
                hum.volume = 0.6f;
                hum.minDistance = 1f;
                hum.maxDistance = 15f;
                hum.rolloffMode = AudioRolloffMode.Linear;
                MakeTrigger(heart, 0.7f, Vector3.zero);
                var col = heart.AddComponent<Collectible>();
                col.type = CollectibleType.Heart;
                col.visual = visual;
                col.burstPrefab = pickupBurst;
                col.burstColor = EditorUtil.Hex("2EE6D6");
                heart.AddComponent<HeartOfEternity>().glow = glow;
                Save(go, "HeartAltar");
            }

            // ---------------- traps
            {
                var go = new GameObject("SpikeTile");
                go.AddComponent<MeshFilter>().sharedMesh = Me("FloorTile");
                go.AddComponent<MeshRenderer>().sharedMaterial = M("Stone_Floor");
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.1f, 0f);
                col.size = new Vector3(c, 0.2f, c);
                var spikes = new GameObject("Spikes").transform;
                spikes.SetParent(go.transform, false);
                for (int x = -1; x <= 1; x++)
                {
                    for (int z = -1; z <= 1; z++)
                    {
                        var p = new Vector3(x * c / 3f, 0f, z * c / 3f);
                        Visual($"Hole_{x}_{z}", go.transform, "Cylinder_12", "Hole", p + new Vector3(0f, 0.004f, 0f), new Vector3(0.38f, 0.004f, 0.38f), default, false);
                        Visual($"Spike_{x}_{z}", spikes, "Spike", "Metal", p, new Vector3(1.4f, 1.4f, 1.4f));
                    }
                }
                go.AddComponent<SpikeTrap>().spikes = spikes;
                Save(go, "SpikeTile");
            }
            GameObject dartPrefab;
            {
                // Dart: no collider, it sphere-casts ahead each frame
                var go = new GameObject("Dart");
                Visual("Mesh", go.transform, "Dart", "Metal", Vector3.zero, Vector3.one * 1.3f, default, false);
                go.AddComponent<Dart>();
                dartPrefab = Save(go, "Dart");
            }
            {
                var go = new GameObject("DartPlate");
                go.AddComponent<MeshFilter>().sharedMesh = Me("FloorTile");
                go.AddComponent<MeshRenderer>().sharedMaterial = M("Stone_Floor");
                var col = go.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, -0.1f, 0f);
                col.size = new Vector3(c, 0.2f, c);
                var plate = Visual("Plate", go.transform, "Plate", "Stone_Dark", new Vector3(0f, 0.02f, 0f));
                Visual("PlateGlyph", plate.transform, "Quad", "Glyph_Wall_4", new Vector3(0f, 0.061f, 0f), Vector3.one * c * 0.4f, new Vector3(90f, 0f, 0f), false);
                var trap = go.AddComponent<DartTrap>();
                trap.plate = plate.transform;
                trap.dartPrefab = dartPrefab;
                Save(go, "DartPlate");
            }
            {
                var go = new GameObject("DartLauncher"); // fires along local +Z
                Visual("Block", go.transform, "LauncherBlock", "Stone_Dark", Vector3.zero);
                Visual("Hole", go.transform, "Cylinder_12", "Hole", new Vector3(0f, 0f, 0.301f), new Vector3(0.16f, 0.01f, 0.16f), new Vector3(90f, 0f, 0f), false);
                Save(go, "DartLauncher");
            }
            {
                // Boulder: no collider (it damages by distance), rumble loop starts when released
                var go = new GameObject("Boulder");
                float radius = c * 0.38f;
                Visual("Mesh", go.transform, "Rock_Boulder", "Rock", Vector3.zero, Vector3.one * radius);
                var rumble = go.AddComponent<AudioSource>();
                rumble.clip = AudioGenerator.Load("boulder_roll");
                rumble.loop = true;
                rumble.playOnAwake = false;
                rumble.spatialBlend = 1f;
                rumble.minDistance = 3f;
                rumble.maxDistance = 30f;
                rumble.rolloffMode = AudioRolloffMode.Linear;
                Save(go, "Boulder");
            }
            {
                // Falling rock (no collider: it damages by distance on impact, so it never shoves the player)
                var go = new GameObject("Debris");
                var rock = Visual("Mesh", go.transform, "Rock_0", "Rock", Vector3.zero, Vector3.one * 0.7f);
                var debris = go.AddComponent<FallingDebris>();
                debris.rock = rock.transform;
                debris.warningPrefab = warningCircle;
                debris.dustBurstPrefab = dustBurst;
                Save(go, "Debris");
            }

            // ---------------- decoration (no colliders except the pillar root)
            {
                var go = new GameObject("Vine"); // pivot at the top, hangs down
                Visual("Leaves", go.transform, "Quad", "Moss", new Vector3(0f, -1.5f, 0f), new Vector3(1.2f, 3f, 1f), default, false);
                Visual("Leaves2", go.transform, "Quad", "Moss", new Vector3(0.35f, -1.1f, -0.01f), new Vector3(0.8f, 2.2f, 1f), new Vector3(0f, 0f, 4f), false);
                Save(go, "Vine");
            }
            {
                var go = new GameObject("GlyphPanel"); // faces local -Z
                Visual("Panel", go.transform, "Quad", "Glyph_Wall_1", Vector3.zero, Vector3.one * 1.3f, default, false);
                Save(go, "GlyphPanel");
            }
            {
                var go = new GameObject("Pillar");
                Visual("Base", go.transform, "PillarBase", "Stone_Dark", Vector3.zero);
                Visual("Shaft", go.transform, "Cylinder_12", "Stone_Wall", new Vector3(0f, 1.35f, 0f), new Vector3(0.7f, 2.2f, 0.7f));
                Visual("BrokenTop", go.transform, "Rock_1", "Stone_Dark", new Vector3(0.05f, 2.5f, 0f), new Vector3(0.8f, 0.5f, 0.8f), new Vector3(8f, 30f, 12f));
                Visual("FallenChunk", go.transform, "Rock_2", "Stone_Dark", new Vector3(0.8f, 0.2f, 0.5f), new Vector3(0.6f, 0.45f, 0.6f), new Vector3(0f, 70f, 20f));
                var cap = go.AddComponent<CapsuleCollider>();
                cap.center = new Vector3(0f, 1.4f, 0f);
                cap.radius = 0.45f;
                cap.height = 2.8f;
                Save(go, "Pillar");
            }
            {
                var go = new GameObject("Rubble");
                var rnd = new System.Random(77);
                string[] rocks = { "Rock_0", "Rock_1", "Rock_2" };
                for (int i = 0; i < 6; i++)
                {
                    float size = 0.25f + (float)rnd.NextDouble() * 0.45f;
                    var pos = new Vector3((float)rnd.NextDouble() * 1.6f - 0.8f, size * 0.3f, (float)rnd.NextDouble() * 1.6f - 0.8f);
                    Visual("Rock_" + i, go.transform, rocks[i % 3], "Rock", pos, Vector3.one * size, new Vector3(0f, rnd.Next(360), rnd.Next(30)));
                }
                Save(go, "Rubble");
            }

            // ---------------- player
            {
                var go = ExplorerBuilder.Build(ExplorerBuilder.CreateMaterials(), Vector3.zero, Quaternion.identity);
                Save(go, "Player");
            }

            AssetDatabase.SaveAssets();
        }

        static Transform DoorSlab(Transform root, string material)
        {
            var s = GameSettingsAsset.GetOrCreate();
            var slab = Visual("Slab", root, "DoorSlab", material, Vector3.zero).transform;
            var col = slab.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, s.wallHeight * 0.5f, 0f);
            col.size = new Vector3(s.cellSize, s.wallHeight, 0.8f);
            return slab;
        }

        /// <summary>The bit of "outside" behind an entrance/exit (local +Z): sand floor (walkable), side walls.</summary>
        static Transform Outside(Transform root, float c, float h, bool daylightGlow)
        {
            var outside = new GameObject("Outside").transform;
            outside.SetParent(root, false);
            var sand = Visual("SandFloor", outside, "FloorTile", "Sand", new Vector3(0f, 0f, c));
            var col = sand.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, -0.1f, 0f);
            col.size = new Vector3(c, 0.2f, c);
            Visual("SideWall_L", outside, "WallBlock", "Stone_Wall", new Vector3(-c, 0f, c));
            Visual("SideWall_R", outside, "WallBlock", "Stone_Wall", new Vector3(c, 0f, c));
            if (daylightGlow)
                Visual("DaylightGlow", outside, "Quad", "Daylight", new Vector3(0f, h * 0.5f, c * 1.45f), new Vector3(c * 1.2f, h * 1.2f, 1f), default, false);
            return outside;
        }

        static void Blocker(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Blocker");
            go.transform.SetParent(parent, false);
            var box = go.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
        }

        /// <summary>A one-shot particle burst prefab (does not play until Effects.Spawn calls Play).</summary>
        static GameObject BurstPrefab(string name, string material, int count, float lifeMin, float lifeMax,
            float speedMin, float speedMax, float sizeMin, float sizeMax, float gravity, bool grow)
        {
            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = lifeMax;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, grow ? AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f) : AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = M(material);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return Save(go, name);
        }

        static void KeyPrefab(string name, KeyType key, Color glow, GameObject burst)
        {
            var go = new GameObject(name);
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            visual.localPosition = new Vector3(0f, 1.1f, 0f);
            Visual("Tablet", visual, "Tablet", "Stone_Dark", Vector3.zero);
            Visual("Glyph_Front", visual, "Quad", name, new Vector3(0f, 0f, -0.051f), Vector3.one * 0.45f, default, false);
            Visual("Glyph_Back", visual, "Quad", name, new Vector3(0f, 0f, 0.051f), Vector3.one * 0.45f, new Vector3(0f, 180f, 0f), false);
            AddLight(go.transform, new Vector3(0f, 1.1f, 0f), glow, 6f, 2.5f, false); // bright enough to spot down a corridor
            MakeTrigger(go, 0.7f, new Vector3(0f, 1.1f, 0f));
            var col = go.AddComponent<Collectible>();
            col.type = CollectibleType.Key;
            col.keyType = key;
            col.visual = visual;
            col.burstPrefab = burst;
            col.burstColor = glow;
            Save(go, name);
        }
    }
}
