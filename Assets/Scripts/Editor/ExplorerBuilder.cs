// ExplorerBuilder: builds the player character entirely from Unity primitives (capsules, spheres,
// cylinders, cubes): legs and arms on pivots so they can swing, a hat, a satchel, and a torch in the
// right hand with a flame particle system, embers, a point light and a crackle sound source.
// It also adds and wires all player scripts (controller, torch, health, inventory, animator).
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static LastTorch.EditorTools.EditorUtil;

namespace LastTorch.EditorTools
{
    public static class ExplorerBuilder
    {
        public class ExplorerMaterials
        {
            public Material skin, shirt, pants, hat, leather, boots, wood, eye, fire, ember;
        }

        /// <summary>Plain-colour explorer materials (Phase 2's MaterialFactory may add textures to the same files).</summary>
        public static ExplorerMaterials CreateMaterials()
        {
            var soft = TextureGenerator.Load("soft_particle");
            if (soft == null) soft = TextureGenerator.SoftParticle();
            return new ExplorerMaterials
            {
                skin = MaterialFactory.Lit("Explorer_Skin", Hex("C68B59"), 0.35f),
                shirt = MaterialFactory.Lit("Explorer_Cloth", Hex("B9A27A"), 0.15f),
                pants = MaterialFactory.Lit("Explorer_Pants", Hex("6B5B45"), 0.1f),
                hat = MaterialFactory.Lit("Explorer_Hat", Hex("6B4226"), 0.2f),
                leather = MaterialFactory.Lit("Explorer_Leather", Hex("5A3A22"), 0.35f),
                boots = MaterialFactory.Lit("Explorer_Boots", Hex("3A2A1E"), 0.3f),
                wood = MaterialFactory.Wood(),
                eye = MaterialFactory.Lit("Explorer_Eye", Hex("1A1410"), 0.8f),
                fire = MaterialFactory.Particle("Particle_Fire", soft, new Color(1f, 0.55f, 0.2f, 1f), true),
                ember = MaterialFactory.Particle("Particle_Ember", soft, new Color(1f, 0.7f, 0.3f, 1f), true),
            };
        }

        /// <summary>Creates the full Player GameObject in the open scene.</summary>
        public static GameObject Build(ExplorerMaterials m, Vector3 position, Quaternion rotation)
        {
            var root = new GameObject("Player");
            root.tag = "Player";
            root.transform.SetPositionAndRotation(position, rotation);

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.skinWidth = 0.05f;
            var controller = root.AddComponent<PlayerController>();

            // The torch light sits inside the body, so the body itself casts no shadows.
            const bool shadows = false;

            var model = Empty("Model", root.transform, Vector3.zero).transform;
            var hips = Empty("Hips", model, new Vector3(0f, 0.9f, 0f)).transform;

            // Legs (pivot at the hip joint)
            var leftLeg = Empty("LeftLeg", hips, new Vector3(-0.11f, 0f, 0f)).transform;
            Part(PrimitiveType.Capsule, "LeftLegMesh", leftLeg, new Vector3(0f, -0.42f, 0f), new Vector3(0.16f, 0.42f, 0.16f), m.pants, default, shadows);
            Part(PrimitiveType.Cube, "LeftBoot", leftLeg, new Vector3(0f, -0.83f, 0.03f), new Vector3(0.15f, 0.12f, 0.24f), m.boots, default, shadows);
            var rightLeg = Empty("RightLeg", hips, new Vector3(0.11f, 0f, 0f)).transform;
            Part(PrimitiveType.Capsule, "RightLegMesh", rightLeg, new Vector3(0f, -0.42f, 0f), new Vector3(0.16f, 0.42f, 0.16f), m.pants, default, shadows);
            Part(PrimitiveType.Cube, "RightBoot", rightLeg, new Vector3(0f, -0.83f, 0.03f), new Vector3(0.15f, 0.12f, 0.24f), m.boots, default, shadows);

            // Torso, belt, satchel
            var torso = Empty("Torso", hips, new Vector3(0f, 0.05f, 0f)).transform;
            Part(PrimitiveType.Capsule, "TorsoMesh", torso, new Vector3(0f, 0.3f, 0f), new Vector3(0.42f, 0.32f, 0.28f), m.shirt, default, shadows);
            Part(PrimitiveType.Cylinder, "Belt", torso, new Vector3(0f, 0.06f, 0f), new Vector3(0.43f, 0.03f, 0.29f), m.leather, default, shadows);
            Part(PrimitiveType.Cube, "StrapFront", torso, new Vector3(0.02f, 0.32f, 0.145f), new Vector3(0.05f, 0.72f, 0.02f), m.leather, new Vector3(0f, 0f, 35f), shadows);
            Part(PrimitiveType.Cube, "StrapBack", torso, new Vector3(0.02f, 0.32f, -0.145f), new Vector3(0.05f, 0.72f, 0.02f), m.leather, new Vector3(0f, 0f, 35f), shadows);
            Part(PrimitiveType.Cube, "Satchel", torso, new Vector3(-0.25f, 0.06f, 0.02f), new Vector3(0.1f, 0.22f, 0.24f), m.leather, default, shadows);

            // Head + hat + eyes
            var head = Empty("Head", torso, new Vector3(0f, 0.66f, 0f)).transform;
            Part(PrimitiveType.Sphere, "HeadMesh", head, new Vector3(0f, 0.12f, 0f), Vector3.one * 0.26f, m.skin, default, shadows);
            Part(PrimitiveType.Sphere, "LeftEye", head, new Vector3(-0.055f, 0.15f, 0.115f), Vector3.one * 0.035f, m.eye, default, shadows);
            Part(PrimitiveType.Sphere, "RightEye", head, new Vector3(0.055f, 0.15f, 0.115f), Vector3.one * 0.035f, m.eye, default, shadows);
            Part(PrimitiveType.Cylinder, "HatBrim", head, new Vector3(0f, 0.21f, 0f), new Vector3(0.44f, 0.012f, 0.44f), m.hat, default, shadows);
            Part(PrimitiveType.Cylinder, "HatCrown", head, new Vector3(0f, 0.27f, 0f), new Vector3(0.25f, 0.065f, 0.25f), m.hat, default, shadows);
            Part(PrimitiveType.Cylinder, "HatBand", head, new Vector3(0f, 0.225f, 0f), new Vector3(0.255f, 0.015f, 0.255f), m.leather, default, shadows);

            // Arms (pivot at the shoulder)
            var leftArm = Empty("LeftArm", torso, new Vector3(-0.27f, 0.55f, 0f)).transform;
            Part(PrimitiveType.Capsule, "LeftArmMesh", leftArm, new Vector3(0f, -0.27f, 0f), new Vector3(0.12f, 0.28f, 0.12f), m.shirt, default, shadows);
            Part(PrimitiveType.Sphere, "LeftHand", leftArm, new Vector3(0f, -0.58f, 0f), Vector3.one * 0.1f, m.skin, default, shadows);
            var rightArm = Empty("RightArm", torso, new Vector3(0.27f, 0.55f, 0f)).transform;
            Part(PrimitiveType.Capsule, "RightArmMesh", rightArm, new Vector3(0f, -0.27f, 0f), new Vector3(0.12f, 0.28f, 0.12f), m.shirt, default, shadows);
            Part(PrimitiveType.Sphere, "RightHand", rightArm, new Vector3(0f, -0.58f, 0f), Vector3.one * 0.1f, m.skin, default, shadows);

            // Torch in the right hand
            var torch = Empty("Torch", rightArm, new Vector3(0f, -0.6f, 0.03f)).transform;
            Part(PrimitiveType.Cylinder, "Handle", torch, new Vector3(0f, 0.12f, 0f), new Vector3(0.045f, 0.22f, 0.045f), m.wood, default, shadows);
            Part(PrimitiveType.Cylinder, "Wrap", torch, new Vector3(0f, 0.33f, 0f), new Vector3(0.075f, 0.05f, 0.075f), m.leather, default, shadows);
            var flame = CreateFlame(torch, new Vector3(0f, 0.4f, 0f), m.fire);
            var embers = CreateEmbers(torch, new Vector3(0f, 0.42f, 0f), m.ember);

            var lightGo = Empty("TorchLight", torch, new Vector3(0f, 0.55f, 0f));
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 14f;
            light.intensity = 3f;
            light.color = new Color(1f, 0.62f, 0.28f);
            light.shadows = LightShadows.Soft;
            light.shadowNearPlane = 0.1f;

            var crackle = lightGo.AddComponent<AudioSource>();
            crackle.playOnAwake = false;
            crackle.loop = true;
            crackle.spatialBlend = 1f;
            crackle.minDistance = 1f;
            crackle.maxDistance = 15f;
            crackle.rolloffMode = AudioRolloffMode.Linear;

            // Player scripts
            var torchSystem = root.AddComponent<TorchSystem>();
            torchSystem.torchLight = light;
            torchSystem.flame = flame;
            torchSystem.embers = embers;
            torchSystem.crackleSource = crackle;

            var health = root.AddComponent<PlayerHealth>();
            health.model = model;

            root.AddComponent<Inventory>();

            var animator = root.AddComponent<ProceduralAnimator>();
            animator.hips = hips;
            animator.torso = torso;
            animator.leftArm = leftArm;
            animator.rightArm = rightArm;
            animator.leftLeg = leftLeg;
            animator.rightLeg = rightLeg;
            animator.torchHolder = torch;

            return root;
        }

        public static ParticleSystem CreateFlame(Transform parent, Vector3 localPos, Material material)
        {
            var go = Empty("Flame", parent, localPos);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            main.startSize = 0.28f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;

            var emission = ps.emission;
            emission.rateOverTime = 45f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.04f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // emit upward

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.55f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.45f), new GradientColorKey(new Color(0.6f, 0.1f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        public static ParticleSystem CreateEmbers(Transform parent, Vector3 localPos, Material material)
        {
            var go = Empty("Embers", parent, localPos);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 1.0f);
            main.startSize = 0.035f;
            main.startColor = Color.white;
            main.gravityModifier = -0.05f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;

            var emission = ps.emission;
            emission.rateOverTime = 8f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0f, 0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 1.5f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(1f, 0.3f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }
    }
}
