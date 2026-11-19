// LevelBuilder: turns maze.txt into the 3D temple. Each character is one cellSize x cellSize cell
// (row 0 = north = +Z). Walls become ONE combined mesh (only faces you can see, world-space UVs so the
// stone tiles evenly) plus one box collider per horizontal run of '#'. The floor is one mesh + one
// collider. Then it places the spawn, oil jars, gems, keys, doors, the Heart altar and the exit, and
// adds seeded decoration (vines, glowing glyphs, rubble in dead ends, broken pillars in the Sanctum).
// Edit maze.txt (project root or Assets/Resources) and run "2. Build Scenes" to change the layout.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastTorch.EditorTools
{
    public static class LevelBuilder
    {
        public const string ResourcesMazePath = "Assets/Resources/maze.txt";
        const int DecorationSeed = 1234;

        public class LevelInfo
        {
            public Vector3 spawnPosition;
            public Quaternion spawnRotation = Quaternion.identity;
            public ExitGate exitGate;
            public int gems, oilJars, keys;
        }

        // Grid directions: north, east, south, west (row offset, column offset)
        static readonly Vector2Int[] Dirs = { new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1) };

        class Grid
        {
            public char[,] cells;
            public int rows, cols;
            public float size;
            public bool InBounds(int r, int c) => r >= 0 && r < rows && c >= 0 && c < cols;
            public char At(int r, int c) => InBounds(r, c) ? cells[r, c] : ' ';
            public bool IsWall(int r, int c) => At(r, c) == '#';
            public bool IsWalkable(int r, int c) => InBounds(r, c) && cells[r, c] != '#';
            public static bool IsDoor(char ch) => ch == 'A' || ch == 'H' || ch == 'D' || ch == 'X';
            public Vector3 World(int r, int c) => new Vector3(c * size, 0f, (rows - 1 - r) * size);
            public static Vector3 DirToWorld(Vector2Int d) => new Vector3(d.y, 0f, -d.x);
        }

        // ------------------------------------------------------------------ maze file

        /// <summary>Copies the project-root maze.txt into Assets/Resources when it is newer (latest edit wins).</summary>
        static void SyncMazeFile()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string rootFile = Path.Combine(projectRoot, "maze.txt");
            string resFile = Path.Combine(projectRoot, ResourcesMazePath);
            if (File.Exists(rootFile) && (!File.Exists(resFile) || File.GetLastWriteTimeUtc(rootFile) > File.GetLastWriteTimeUtc(resFile)))
            {
                EditorUtil.EnsureFolder("Assets/Resources");
                File.Copy(rootFile, resFile, true);
                AssetDatabase.ImportAsset(ResourcesMazePath, ImportAssetOptions.ForceUpdate);
                Debug.Log("[LevelBuilder] Copied maze.txt into Assets/Resources.");
            }
            if (!File.Exists(resFile)) throw new Exception("maze.txt not found (project root or Assets/Resources).");
        }

        static Grid LoadGrid(float cellSize)
        {
            SyncMazeFile();
            string text = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), ResourcesMazePath));
            var lines = new List<string>();
            foreach (var raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Trim().Length > 0) lines.Add(line);
            }
            int cols = 0;
            foreach (var l in lines) cols = Mathf.Max(cols, l.Length);

            var grid = new Grid { rows = lines.Count, cols = cols, size = cellSize, cells = new char[lines.Count, cols] };
            const string known = "#.SDOg123AH*X^TBb";
            for (int r = 0; r < grid.rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    char ch = c < lines[r].Length ? lines[r][c] : '#';
                    if (ch == ' ') ch = '#';
                    if (known.IndexOf(ch) < 0)
                    {
                        Debug.LogWarning($"[LevelBuilder] Unknown character '{ch}' at row {r}, column {c}; treated as floor.");
                        ch = '.';
                    }
                    grid.cells[r, c] = ch;
                }
            }
            return grid;
        }

        // ------------------------------------------------------------------ build

        public static LevelInfo Build(GameSettings s, Transform root)
        {
            var grid = LoadGrid(s.cellSize);
            var info = new LevelInfo();
            var rnd = new System.Random(DecorationSeed);
            float c = grid.size, h = s.wallHeight;

            var faces = BuildWalls(grid, h, root);
            BuildFloor(grid, root);

            // Spawn
            Vector2Int spawn = Find(grid, 'S');
            if (spawn.x < 0) throw new Exception("maze.txt has no 'S' (player spawn).");
            info.spawnPosition = grid.World(spawn.x, spawn.y);
            info.spawnRotation = SpawnFacing(grid, spawn);

            var reachable = Flood(grid, spawn, blockDoors: true);

            var items = new GameObject("Items").transform;
            items.SetParent(root, false);
            var doors = new GameObject("Doors").transform;
            doors.SetParent(root, false);

            for (int r = 0; r < grid.rows; r++)
            {
                for (int col = 0; col < grid.cols; col++)
                {
                    char ch = grid.cells[r, col];
                    Vector3 pos = grid.World(r, col);
                    switch (ch)
                    {
                        case 'O': SceneBuilder.Instantiate("OilJar", pos, Quaternion.identity, items); info.oilJars++; break;
                        case 'g': SceneBuilder.Instantiate("Gem", pos, Quaternion.identity, items); info.gems++; break;
                        case '1': SceneBuilder.Instantiate("Key_Sun", pos, Quaternion.identity, items); info.keys++; break;
                        case '2': SceneBuilder.Instantiate("Key_Moon", pos, Quaternion.identity, items); info.keys++; break;
                        case '3': SceneBuilder.Instantiate("Key_Serpent", pos, Quaternion.identity, items); info.keys++; break;
                        case '*': SceneBuilder.Instantiate("HeartAltar", pos, Quaternion.identity, items); break;
                        case 'A':
                        {
                            var door = SceneBuilder.Instantiate("KeyDoor", pos, DoorRotation(grid, r, col, reachable), doors);
                            door.GetComponent<KeyDoor>().requiredKey = KeyType.Sun;
                            break;
                        }
                        case 'H': SceneBuilder.Instantiate("SanctumDoor", pos, DoorRotation(grid, r, col, reachable), doors); break;
                        case 'D': SceneBuilder.Instantiate("EntranceDoor", pos, DoorRotation(grid, r, col, null), doors); break;
                        case 'X':
                            info.exitGate = SceneBuilder.Instantiate("ExitGate", pos, DoorRotation(grid, r, col, null), doors).GetComponent<ExitGate>();
                            break;
                        // '^', 'T', 'B', 'b' (traps) are plain floor until Phase 4 adds the trap builders.
                    }
                }
            }

            Decorate(grid, faces, rnd, s, root);
            CheckReachability(grid, spawn);

            Debug.Log($"[LevelBuilder] Built {grid.cols}x{grid.rows} maze: {info.gems} gems, {info.oilJars} oil jars, {info.keys} keys.");
            return info;
        }

        static Vector2Int Find(Grid grid, char ch)
        {
            for (int r = 0; r < grid.rows; r++)
                for (int c = 0; c < grid.cols; c++)
                    if (grid.cells[r, c] == ch) return new Vector2Int(r, c);
            return new Vector2Int(-1, -1);
        }

        /// <summary>The player faces away from the entrance door if it is next to the spawn, else north.</summary>
        static Quaternion SpawnFacing(Grid grid, Vector2Int spawn)
        {
            foreach (var d in Dirs)
                if (grid.At(spawn.x + d.x, spawn.y + d.y) == 'D')
                    return Quaternion.LookRotation(-Grid.DirToWorld(d));
            return Quaternion.identity;
        }

        /// <summary>
        /// Door prefabs face local -Z toward the player's approach side. For key/sanctum doors that is the
        /// side reachable from the spawn; for the entrance/exit it is the side inside the maze.
        /// </summary>
        static Quaternion DoorRotation(Grid grid, int r, int c, HashSet<Vector2Int> reachable)
        {
            Vector2Int front = Vector2Int.zero;
            foreach (var d in Dirs)
            {
                var n = new Vector2Int(r + d.x, c + d.y);
                if (!grid.IsWalkable(n.x, n.y)) continue;
                if (reachable == null || reachable.Contains(n)) { front = d; break; }
                if (front == Vector2Int.zero) front = d;
            }
            if (front == Vector2Int.zero) front = Dirs[0];
            return Quaternion.LookRotation(-Grid.DirToWorld(front));
        }

        static HashSet<Vector2Int> Flood(Grid grid, Vector2Int start, bool blockDoors)
        {
            var seen = new HashSet<Vector2Int> { start };
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var d in Dirs)
                {
                    var n = new Vector2Int(p.x + d.x, p.y + d.y);
                    if (seen.Contains(n) || !grid.IsWalkable(n.x, n.y)) continue;
                    if (blockDoors && Grid.IsDoor(grid.cells[n.x, n.y])) continue;
                    seen.Add(n);
                    queue.Enqueue(n);
                }
            }
            return seen;
        }

        static void CheckReachability(Grid grid, Vector2Int spawn)
        {
            var all = Flood(grid, spawn, blockDoors: false);
            int unreachable = 0;
            for (int r = 0; r < grid.rows; r++)
                for (int c = 0; c < grid.cols; c++)
                    if (grid.IsWalkable(r, c) && !all.Contains(new Vector2Int(r, c))) unreachable++;
            if (unreachable > 0) Debug.LogWarning($"[LevelBuilder] {unreachable} floor cells can never be reached from the spawn.");
        }

        // ------------------------------------------------------------------ geometry

        struct WallFace
        {
            public Vector3 center;   // middle of the face at floor level
            public Vector3 normal;   // points into the corridor
            public Vector3 along;    // horizontal direction along the face
            public char neighbour;   // what is in front of the face
        }

        static List<WallFace> BuildWalls(Grid grid, float h, Transform root)
        {
            var mb = new MeshGenerator.MeshBuilder();
            var faces = new List<WallFace>();
            float c = grid.size, half = c * 0.5f, T = MeshGenerator.TextureWorldSize;

            for (int r = 0; r < grid.rows; r++)
            {
                for (int col = 0; col < grid.cols; col++)
                {
                    if (!grid.IsWall(r, col)) continue;
                    Vector3 p = grid.World(r, col);
                    float x0 = p.x - half, x1 = p.x + half, z0 = p.z - half, z1 = p.z + half;
                    Vector3 center = p + Vector3.up * (h * 0.5f);

                    // Top
                    mb.AddQuadOutward(new Vector3(x0, h, z0), new Vector3(x0, h, z1), new Vector3(x1, h, z1), new Vector3(x1, h, z0), center,
                        new Vector2(x0, z0) / T, new Vector2(x0, z1) / T, new Vector2(x1, z1) / T, new Vector2(x1, z0) / T);

                    // Sides, only where the neighbour is not a wall
                    foreach (var d in Dirs)
                    {
                        if (grid.IsWall(r + d.x, col + d.y)) continue;
                        Vector3 n = Grid.DirToWorld(d);
                        Vector3 along = new Vector3(-n.z, 0f, n.x);
                        Vector3 fc = p + n * half;
                        Vector3 a = fc - along * half, b = fc + along * half;
                        // world-space UV: horizontal = coordinate along the face, vertical = height
                        float ua = Mathf.Abs(n.x) > 0.5f ? a.z : a.x, ub = Mathf.Abs(n.x) > 0.5f ? b.z : b.x;
                        mb.AddQuadOutward(a, a + Vector3.up * h, b + Vector3.up * h, b, center,
                            new Vector2(ua / T, 0f), new Vector2(ua / T, h / T), new Vector2(ub / T, h / T), new Vector2(ub / T, 0f));

                        if (grid.IsWalkable(r + d.x, col + d.y))
                            faces.Add(new WallFace { center = fc, normal = n, along = along, neighbour = grid.cells[r + d.x, col + d.y] });
                    }
                }
            }

            var walls = new GameObject("Walls");
            walls.transform.SetParent(root, false);
            walls.AddComponent<MeshFilter>().sharedMesh = MeshGenerator.Save(mb.ToMesh("Temple_Walls"));
            walls.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Load("Stone_Wall");
            GameObjectUtility.SetStaticEditorFlags(walls, StaticEditorFlags.BatchingStatic);

            // Colliders: one box per horizontal run of '#'
            var colliders = new GameObject("WallColliders").transform;
            colliders.SetParent(root, false);
            for (int r = 0; r < grid.rows; r++)
            {
                int col = 0;
                while (col < grid.cols)
                {
                    if (!grid.IsWall(r, col)) { col++; continue; }
                    int start = col;
                    while (col < grid.cols && grid.IsWall(r, col)) col++;
                    int end = col - 1;
                    var box = new GameObject($"Run_{r}_{start}");
                    box.transform.SetParent(colliders, false);
                    box.transform.position = (grid.World(r, start) + grid.World(r, end)) * 0.5f + Vector3.up * (h * 0.5f);
                    box.AddComponent<BoxCollider>().size = new Vector3((end - start + 1) * c, h, c);
                    box.isStatic = true;
                }
            }
            return faces;
        }

        static void BuildFloor(Grid grid, Transform root)
        {
            var mb = new MeshGenerator.MeshBuilder();
            float half = grid.size * 0.5f, T = MeshGenerator.TextureWorldSize;
            for (int r = 0; r < grid.rows; r++)
            {
                for (int c = 0; c < grid.cols; c++)
                {
                    if (!grid.IsWalkable(r, c)) continue;
                    Vector3 p = grid.World(r, c);
                    float x0 = p.x - half, x1 = p.x + half, z0 = p.z - half, z1 = p.z + half;
                    mb.AddQuadOutward(new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), new Vector3(x1, 0f, z0), p + Vector3.down,
                        new Vector2(x0, z0) / T, new Vector2(x0, z1) / T, new Vector2(x1, z1) / T, new Vector2(x1, z0) / T);
                }
            }
            var floor = new GameObject("Floor");
            floor.transform.SetParent(root, false);
            floor.AddComponent<MeshFilter>().sharedMesh = MeshGenerator.Save(mb.ToMesh("Temple_Floor"));
            floor.AddComponent<MeshRenderer>().sharedMaterial = MaterialFactory.Load("Stone_Floor");
            var box = floor.AddComponent<BoxCollider>();
            Vector3 min = grid.World(grid.rows - 1, 0), max = grid.World(0, grid.cols - 1);
            box.center = (min + max) * 0.5f + Vector3.down * 0.1f;
            box.size = new Vector3(grid.cols * grid.size, 0.2f, grid.rows * grid.size);
            GameObjectUtility.SetStaticEditorFlags(floor, StaticEditorFlags.BatchingStatic);
        }

        // ------------------------------------------------------------------ decoration

        static void Decorate(Grid grid, List<WallFace> faces, System.Random rnd, GameSettings s, Transform root)
        {
            var deco = new GameObject("Decoration").transform;
            deco.SetParent(root, false);
            float c = grid.size, h = s.wallHeight;
            float R(float min, float max) => min + (float)rnd.NextDouble() * (max - min);

            // Vines (~15%) and faint glowing glyph panels (~8%) on wall faces (not next to doors)
            foreach (var f in faces)
            {
                double roll = rnd.NextDouble();
                if (Grid.IsDoor(f.neighbour)) continue;
                Quaternion facing = Quaternion.LookRotation(-f.normal); // quads are visible from their -Z side
                if (roll < 0.15)
                {
                    Vector3 pos = f.center + f.normal * 0.02f + f.along * R(-c * 0.3f, c * 0.3f) + Vector3.up * h;
                    SceneBuilder.Instantiate("Vine", pos, facing, deco);
                }
                else if (roll < 0.23)
                {
                    Vector3 pos = f.center + f.normal * 0.015f + f.along * R(-c * 0.2f, c * 0.2f) + Vector3.up * R(1.6f, 2.6f);
                    var panel = SceneBuilder.Instantiate("GlyphPanel", pos, facing, deco);
                    panel.GetComponentInChildren<MeshRenderer>().sharedMaterial = MaterialFactory.Load("Glyph_Wall_" + (1 + rnd.Next(4)));
                }
            }

            // Rubble in dead ends (plain floor cells with exactly one open neighbour)
            for (int r = 0; r < grid.rows; r++)
            {
                for (int col = 0; col < grid.cols; col++)
                {
                    if (grid.cells[r, col] != '.') continue;
                    int open = 0;
                    Vector2Int openDir = Vector2Int.zero;
                    foreach (var d in Dirs)
                        if (grid.IsWalkable(r + d.x, col + d.y)) { open++; openDir = d; }
                    if (open != 1) continue;
                    Vector3 pos = grid.World(r, col) - Grid.DirToWorld(openDir) * (c * 0.25f);
                    SceneBuilder.Instantiate("Rubble", pos, Quaternion.Euler(0f, rnd.Next(360), 0f), deco);
                }
            }

            // Broken pillars in the corners of the Inner Sanctum (the room around the Heart)
            Vector2Int heart = Find(grid, '*');
            if (heart.x >= 0)
            {
                foreach (var cell in Flood(grid, heart, blockDoors: true))
                {
                    bool n = grid.IsWall(cell.x - 1, cell.y), sth = grid.IsWall(cell.x + 1, cell.y);
                    bool e = grid.IsWall(cell.x, cell.y + 1), w = grid.IsWall(cell.x, cell.y - 1);
                    if (!((n || sth) && (e || w)) || grid.cells[cell.x, cell.y] != '.') continue;
                    Vector3 toCorner = new Vector3(e ? 1f : -1f, 0f, n ? 1f : -1f);
                    SceneBuilder.Instantiate("Pillar", grid.World(cell.x, cell.y) + toCorner * (c * 0.22f), Quaternion.Euler(0f, rnd.Next(360), 0f), deco);
                }
            }
        }
    }
}
