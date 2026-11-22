// MeshGenerator: builds every custom 3D mesh from vertices and triangles in code and saves them as
// .asset files in Assets/Generated/Meshes: gem (octahedron), Heart crystal, spike pyramid, cone,
// cylinder, jar (a profile spun around an axis = "lathe"), low-poly rocks (noise-bent icosphere),
// dart, altar, and boxes with world-scaled UVs (walls, floors, doors). Re-running updates the same assets.
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastTorch.EditorTools
{
    public static class MeshGenerator
    {
        public const string Folder = "Assets/Generated/Meshes";
        /// <summary>One texture repeat covers this many metres on walls/floors.</summary>
        public const float TextureWorldSize = 2f;

        public static void GenerateAll()
        {
            var s = GameSettingsAsset.GetOrCreate();
            float c = s.cellSize, h = s.wallHeight;

            Octahedron();
            Crystal();
            Spike();
            Cone("Cone_12", 12, 0.5f, 1f);
            Cylinder("Cylinder_12", 12, 0.5f, 1f);
            Jar();
            Rock("Rock_0", 11, 1, 0.5f, 0.35f, new Vector3(1f, 0.7f, 1f));
            Rock("Rock_1", 22, 1, 0.5f, 0.4f, new Vector3(1.1f, 0.8f, 0.9f));
            Rock("Rock_2", 33, 1, 0.5f, 0.3f, new Vector3(0.9f, 0.9f, 1.1f));
            Rock("Rock_Boulder", 44, 2, 1f, 0.12f, Vector3.one);
            Dart();
            Altar();
            Quad();

            Box("WallBlock", new Vector3(c, h, c), new Vector3(0f, h * 0.5f, 0f));
            Box("FloorTile", new Vector3(c, 0.2f, c), new Vector3(0f, -0.1f, 0f));
            Box("DoorSlab", new Vector3(c, h, 0.8f), new Vector3(0f, h * 0.5f, 0f));
            Box("Plate", new Vector3(c * 0.55f, 0.12f, c * 0.55f), new Vector3(0f, 0f, 0f));
            Box("Tablet", new Vector3(0.55f, 0.65f, 0.1f), Vector3.zero);
            Box("LauncherBlock", new Vector3(0.7f, 0.7f, 0.3f), new Vector3(0f, 0f, 0.15f));
            Box("PillarBase", new Vector3(1.0f, 0.25f, 1.0f), new Vector3(0f, 0.125f, 0f));
        }

        public static string PathOf(string name) => $"{Folder}/{AssetCategories.Mesh(name)}/{name}.asset";
        public static Mesh Load(string name) => AssetDatabase.LoadAssetAtPath<Mesh>(PathOf(name));

        // ------------------------------------------------------------------ builder

        /// <summary>Collects vertices/triangles. Flat triangles get their own vertices (low-poly look).</summary>
        public class MeshBuilder
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Vector2> uvs = new List<Vector2>();
            public readonly List<int> triangles = new List<int>();

            public int AddVertex(Vector3 p, Vector3 n, Vector2 uv)
            {
                vertices.Add(p); normals.Add(n); uvs.Add(uv);
                return vertices.Count - 1;
            }

            public void AddTriangle(int a, int b, int c) { triangles.Add(a); triangles.Add(b); triangles.Add(c); }

            /// <summary>Flat-shaded triangle, flipped if needed so it faces away from 'center' (for convex shapes).</summary>
            public void AddTriOutward(Vector3 a, Vector3 b, Vector3 c, Vector3 center, Vector2 ua, Vector2 ub, Vector2 uc)
            {
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(n, (a + b + c) / 3f - center) < 0f)
                {
                    (b, c) = (c, b);
                    (ub, uc) = (uc, ub);
                    n = -n;
                }
                n.Normalize();
                AddTriangle(AddVertex(a, n, ua), AddVertex(b, n, ub), AddVertex(c, n, uc));
            }

            public void AddTriOutward(Vector3 a, Vector3 b, Vector3 c, Vector3 center)
            {
                AddTriOutward(a, b, c, center, PlanarUV(a), PlanarUV(b), PlanarUV(c));
            }

            static Vector2 PlanarUV(Vector3 p) => new Vector2(p.x + p.z, p.y);

            /// <summary>Quad facing away from 'center', with UVs from world-size / TextureWorldSize.</summary>
            public void AddQuadOutward(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 center, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
            {
                Vector3 n = Vector3.Cross(b - a, c - a);
                bool flip = Vector3.Dot(n, (a + b + c + d) / 4f - center) < 0f;
                if (flip) n = -n;
                n.Normalize();
                int ia = AddVertex(a, n, ua), ib = AddVertex(b, n, ub), ic = AddVertex(c, n, uc), id = AddVertex(d, n, ud);
                if (flip) { AddTriangle(ia, ic, ib); AddTriangle(ia, id, ic); }
                else { AddTriangle(ia, ib, ic); AddTriangle(ia, ic, id); }
            }

            /// <summary>Axis-aligned box. UVs are in metres / TextureWorldSize so textures keep a constant size.</summary>
            public void AddBox(Vector3 center, Vector3 size)
            {
                Vector3 e = size * 0.5f;
                float t = TextureWorldSize;
                Vector3 P(float x, float y, float z) => center + new Vector3(x * e.x, y * e.y, z * e.z);

                // +X / -X faces: UV = (z, y)
                for (int s = -1; s <= 1; s += 2)
                    AddQuadOutward(P(s, -1, -1), P(s, 1, -1), P(s, 1, 1), P(s, -1, 1), center,
                        new Vector2(0, 0), new Vector2(0, size.y / t), new Vector2(size.z / t, size.y / t), new Vector2(size.z / t, 0));
                // +Y / -Y faces: UV = (x, z)
                for (int s = -1; s <= 1; s += 2)
                    AddQuadOutward(P(-1, s, -1), P(-1, s, 1), P(1, s, 1), P(1, s, -1), center,
                        new Vector2(0, 0), new Vector2(0, size.z / t), new Vector2(size.x / t, size.z / t), new Vector2(size.x / t, 0));
                // +Z / -Z faces: UV = (x, y)
                for (int s = -1; s <= 1; s += 2)
                    AddQuadOutward(P(-1, -1, s), P(-1, 1, s), P(1, 1, s), P(1, -1, s), center,
                        new Vector2(0, 0), new Vector2(0, size.y / t), new Vector2(size.x / t, size.y / t), new Vector2(size.x / t, 0));
            }

            /// <summary>
            /// Spins a 2D profile (x = radius, y = height) around the Y axis. The profile must go from the
            /// bottom centre, outward, up, and back to the top centre. smooth = averaged normals between segments.
            /// </summary>
            public void AddLathe(Vector2[] profile, int segments, bool smooth, Vector3 offset)
            {
                int count = profile.Length;
                var lengths = new float[count];
                for (int i = 1; i < count; i++) lengths[i] = lengths[i - 1] + (profile[i] - profile[i - 1]).magnitude;
                float total = Mathf.Max(0.0001f, lengths[count - 1]);

                Vector2 SegNormal(int i)
                {
                    Vector2 d = profile[i + 1] - profile[i];
                    return new Vector2(d.y, -d.x).normalized; // (radial, up)
                }

                if (smooth)
                {
                    int start = vertices.Count;
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 n2 = Vector2.zero;
                        if (i > 0) n2 += SegNormal(i - 1);
                        if (i < count - 1) n2 += SegNormal(i);
                        n2.Normalize();
                        for (int j = 0; j <= segments; j++)
                        {
                            float ang = j / (float)segments * Mathf.PI * 2f;
                            float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
                            AddVertex(offset + new Vector3(profile[i].x * cos, profile[i].y, profile[i].x * sin),
                                new Vector3(n2.x * cos, n2.y, n2.x * sin), new Vector2(j / (float)segments, lengths[i] / total));
                        }
                    }
                    int ring = segments + 1;
                    for (int i = 0; i < count - 1; i++)
                    {
                        for (int j = 0; j < segments; j++)
                        {
                            int a = start + i * ring + j, b = start + (i + 1) * ring + j, c = b + 1, d = a + 1;
                            AddTriangle(a, b, c);
                            AddTriangle(a, c, d);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < count - 1; i++)
                    {
                        Vector2 n2 = SegNormal(i);
                        int start = vertices.Count;
                        for (int k = 0; k < 2; k++)
                        {
                            Vector2 p = profile[i + k];
                            for (int j = 0; j <= segments; j++)
                            {
                                float ang = j / (float)segments * Mathf.PI * 2f;
                                float cos = Mathf.Cos(ang), sin = Mathf.Sin(ang);
                                AddVertex(offset + new Vector3(p.x * cos, p.y, p.x * sin),
                                    new Vector3(n2.x * cos, n2.y, n2.x * sin), new Vector2(j / (float)segments, lengths[i + k] / total));
                            }
                        }
                        int ring = segments + 1;
                        for (int j = 0; j < segments; j++)
                        {
                            int a = start + j, b = start + ring + j, c = b + 1, d = a + 1;
                            AddTriangle(a, b, c);
                            AddTriangle(a, c, d);
                        }
                    }
                }
            }

            /// <summary>Rotates/moves every vertex added after 'fromVertex'.</summary>
            public void Transform(int fromVertex, Quaternion rotation, Vector3 offset)
            {
                for (int i = fromVertex; i < vertices.Count; i++)
                {
                    vertices[i] = rotation * vertices[i] + offset;
                    normals[i] = rotation * normals[i];
                }
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name };
                if (vertices.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }
        }

        /// <summary>Saves the mesh as Folder/name.asset (updates the existing asset so prefab links survive).</summary>
        public static Mesh Save(Mesh mesh)
        {
            EditorUtil.EnsureFolder($"{Folder}/{AssetCategories.Mesh(mesh.name)}");
            string path = PathOf(mesh.name);
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null)
            {
                existing.Clear();
                EditorUtility.CopySerialized(mesh, existing);
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // ------------------------------------------------------------------ shapes

        public static Mesh Box(string name, Vector3 size, Vector3 center)
        {
            var mb = new MeshBuilder();
            mb.AddBox(center, size);
            return Save(mb.ToMesh(name));
        }

        /// <summary>Gem: 8-sided diamond.</summary>
        public static Mesh Octahedron()
        {
            var mb = new MeshBuilder();
            Vector3 top = new Vector3(0f, 0.7f, 0f), bottom = new Vector3(0f, -0.7f, 0f);
            var ring = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float a = i * Mathf.PI * 0.5f;
                ring[i] = new Vector3(Mathf.Cos(a) * 0.5f, 0f, Mathf.Sin(a) * 0.5f);
            }
            for (int i = 0; i < 4; i++)
            {
                mb.AddTriOutward(top, ring[i], ring[(i + 1) % 4], Vector3.zero);
                mb.AddTriOutward(bottom, ring[i], ring[(i + 1) % 4], Vector3.zero);
            }
            return Save(mb.ToMesh("Gem_Octahedron"));
        }

        /// <summary>Heart of Eternity: elongated six-sided crystal.</summary>
        public static Mesh Crystal()
        {
            var mb = new MeshBuilder();
            Vector3 top = new Vector3(0f, 0.9f, 0f), bottom = new Vector3(0f, -0.55f, 0f);
            Vector3 center = new Vector3(0f, 0.1f, 0f);
            var upper = new Vector3[6];
            var lower = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI / 3f;
                upper[i] = new Vector3(Mathf.Cos(a) * 0.28f, 0.4f, Mathf.Sin(a) * 0.28f);
                lower[i] = new Vector3(Mathf.Cos(a) * 0.32f, -0.15f, Mathf.Sin(a) * 0.32f);
            }
            for (int i = 0; i < 6; i++)
            {
                int n = (i + 1) % 6;
                mb.AddTriOutward(top, upper[i], upper[n], center);
                mb.AddTriOutward(upper[i], lower[i], lower[n], center);
                mb.AddTriOutward(upper[i], lower[n], upper[n], center);
                mb.AddTriOutward(bottom, lower[i], lower[n], center);
            }
            return Save(mb.ToMesh("Heart_Crystal"));
        }

        /// <summary>Four-sided spike for spike traps.</summary>
        public static Mesh Spike()
        {
            var mb = new MeshBuilder();
            Vector3 tip = new Vector3(0f, 0.6f, 0f);
            Vector3 center = new Vector3(0f, 0.15f, 0f);
            var b = new[] { new Vector3(-0.09f, 0f, -0.09f), new Vector3(0.09f, 0f, -0.09f), new Vector3(0.09f, 0f, 0.09f), new Vector3(-0.09f, 0f, 0.09f) };
            for (int i = 0; i < 4; i++) mb.AddTriOutward(tip, b[i], b[(i + 1) % 4], center);
            mb.AddTriOutward(b[0], b[1], b[2], center);
            mb.AddTriOutward(b[0], b[2], b[3], center);
            return Save(mb.ToMesh("Spike"));
        }

        public static Mesh Cone(string name, int segments, float radius, float height)
        {
            var mb = new MeshBuilder();
            mb.AddLathe(new[] { new Vector2(0f, 0f), new Vector2(radius, 0f), new Vector2(0f, height) }, segments, false, Vector3.zero);
            return Save(mb.ToMesh(name));
        }

        /// <summary>Cylinder centred on the origin (height along Y), like Unity's primitive but with N sides.</summary>
        public static Mesh Cylinder(string name, int segments, float radius, float height)
        {
            var mb = new MeshBuilder();
            mb.AddLathe(new[] { new Vector2(0f, 0f), new Vector2(radius, 0f), new Vector2(radius, height), new Vector2(0f, height) },
                segments, false, new Vector3(0f, -height * 0.5f, 0f));
            return Save(mb.ToMesh(name));
        }

        /// <summary>Oil jar: a curved clay pot made by spinning a side profile.</summary>
        public static Mesh Jar()
        {
            var profile = new[]
            {
                new Vector2(0f, 0f), new Vector2(0.2f, 0f), new Vector2(0.29f, 0.07f), new Vector2(0.34f, 0.22f),
                new Vector2(0.32f, 0.38f), new Vector2(0.24f, 0.52f), new Vector2(0.14f, 0.6f), new Vector2(0.12f, 0.66f),
                new Vector2(0.17f, 0.72f), new Vector2(0.15f, 0.75f), new Vector2(0f, 0.75f)
            };
            var mb = new MeshBuilder();
            mb.AddLathe(profile, 24, true, Vector3.zero);
            return Save(mb.ToMesh("Jar"));
        }

        /// <summary>Low-poly rock: an icosphere whose vertices are pushed in/out by seeded noise.</summary>
        public static Mesh Rock(string name, int seed, int subdivisions, float radius, float roughness, Vector3 squash)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var verts = new List<Vector3>
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            var faces = new List<int>
            {
                0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1
            };

            for (int s = 0; s < subdivisions; s++)
            {
                var cache = new Dictionary<long, int>();
                int Mid(int a, int b)
                {
                    long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                    if (cache.TryGetValue(key, out int idx)) return idx;
                    verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
                    cache[key] = verts.Count - 1;
                    return verts.Count - 1;
                }
                var next = new List<int>();
                for (int f = 0; f < faces.Count; f += 3)
                {
                    int a = faces[f], b = faces[f + 1], c = faces[f + 2];
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                faces = next;
            }

            var rnd = new System.Random(seed);
            float ox = (float)rnd.NextDouble() * 100f, oy = (float)rnd.NextDouble() * 100f, oz = (float)rnd.NextDouble() * 100f;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 p = verts[i] * 1.7f;
                float n = (Mathf.PerlinNoise(p.x + ox, p.y + oy) + Mathf.PerlinNoise(p.y + oy, p.z + oz) + Mathf.PerlinNoise(p.z + oz, p.x + ox)) / 3f;
                float r = radius * (1f + roughness * (n - 0.5f) * 2f);
                verts[i] = Vector3.Scale(verts[i] * r, squash);
            }

            var mb = new MeshBuilder();
            for (int f = 0; f < faces.Count; f += 3)
                mb.AddTriOutward(verts[faces[f]], verts[faces[f + 1]], verts[faces[f + 2]], Vector3.zero);
            return Save(mb.ToMesh(name));
        }

        /// <summary>Dart pointing along +Z: thin shaft, cone tip and two crossed fins.</summary>
        public static Mesh Dart()
        {
            var mb = new MeshBuilder();
            int start = mb.vertices.Count;
            mb.AddLathe(new[] { new Vector2(0f, 0f), new Vector2(0.018f, 0f), new Vector2(0.018f, 0.36f), new Vector2(0f, 0.36f) }, 6, false, new Vector3(0f, -0.2f, 0f));
            mb.AddLathe(new[] { new Vector2(0f, 0f), new Vector2(0.035f, 0f), new Vector2(0f, 0.12f) }, 6, false, new Vector3(0f, 0.16f, 0f));
            mb.Transform(start, Quaternion.Euler(90f, 0f, 0f), Vector3.zero); // +Y becomes +Z

            // Four fins at the tail (two crossed planes), double-sided so they show from any angle
            foreach (var side in new[] { Vector3.right, Vector3.left, Vector3.up, Vector3.down })
            {
                Vector3 a = new Vector3(0f, 0f, -0.2f);
                Vector3 b = a + side * 0.05f;
                Vector3 c = new Vector3(0f, 0f, -0.1f) + side * 0.015f;
                Vector3 d = new Vector3(0f, 0f, -0.1f);
                AddDoubleSidedQuad(mb, a, b, c, d);
            }
            return Save(mb.ToMesh("Dart"));
        }

        static void AddDoubleSidedQuad(MeshBuilder mb, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            int a1 = mb.AddVertex(a, n, Vector2.zero), b1 = mb.AddVertex(b, n, Vector2.right), c1 = mb.AddVertex(c, n, Vector2.one), d1 = mb.AddVertex(d, n, Vector2.up);
            mb.AddTriangle(a1, b1, c1); mb.AddTriangle(a1, c1, d1);
            int a2 = mb.AddVertex(a, -n, Vector2.zero), b2 = mb.AddVertex(b, -n, Vector2.right), c2 = mb.AddVertex(c, -n, Vector2.one), d2 = mb.AddVertex(d, -n, Vector2.up);
            mb.AddTriangle(a2, c2, b2); mb.AddTriangle(a2, d2, c2);
        }

        /// <summary>Altar for the Heart: three stacked stone blocks. Top surface at y = 1.18.</summary>
        public static Mesh Altar()
        {
            var mb = new MeshBuilder();
            mb.AddBox(new Vector3(0f, 0.15f, 0f), new Vector3(1.8f, 0.3f, 1.8f));
            mb.AddBox(new Vector3(0f, 0.65f, 0f), new Vector3(1.3f, 0.7f, 1.3f));
            mb.AddBox(new Vector3(0f, 1.09f, 0f), new Vector3(1.6f, 0.18f, 1.6f));
            return Save(mb.ToMesh("Altar"));
        }

        /// <summary>1x1 quad in the XY plane, visible from -Z (like Unity's built-in quad).</summary>
        public static Mesh Quad()
        {
            var mb = new MeshBuilder();
            var n = Vector3.back;
            int a = mb.AddVertex(new Vector3(-0.5f, -0.5f, 0f), n, new Vector2(0f, 0f));
            int b = mb.AddVertex(new Vector3(-0.5f, 0.5f, 0f), n, new Vector2(0f, 1f));
            int c = mb.AddVertex(new Vector3(0.5f, 0.5f, 0f), n, new Vector2(1f, 1f));
            int d = mb.AddVertex(new Vector3(0.5f, -0.5f, 0f), n, new Vector2(1f, 0f));
            mb.AddTriangle(a, b, c);
            mb.AddTriangle(a, c, d);
            return Save(mb.ToMesh("Quad"));
        }
    }
}
