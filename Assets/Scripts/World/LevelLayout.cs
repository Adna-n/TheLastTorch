// LevelLayout: a copy of the maze grid stored on the Level object when the scene is built, so
// runtime scripts (the minimap) know where walls, doors and traps are. Also converts between
// world positions and grid cells (row 0 = north, cell (r, c) sits at x = c*cellSize, z = (rows-1-r)*cellSize).
using UnityEngine;

namespace LastTorch
{
    public class LevelLayout : MonoBehaviour
    {
        public int rows;
        public int cols;
        public float cellSize = 4f;
        [Tooltip("One string per maze row, same characters as maze.txt.")]
        public string[] lines;

        public bool InBounds(int r, int c) => r >= 0 && r < rows && c >= 0 && c < cols;

        public char At(int r, int c) => InBounds(r, c) && c < lines[r].Length ? lines[r][c] : '#';

        public Vector2Int WorldToCell(Vector3 p) =>
            new Vector2Int(rows - 1 - Mathf.RoundToInt(p.z / cellSize), Mathf.RoundToInt(p.x / cellSize));

        /// <summary>Position on the map image, 0..1 in both directions (north = up).</summary>
        public Vector2 WorldToMapUV(Vector3 p) =>
            new Vector2((p.x / cellSize + 0.5f) / cols, (p.z / cellSize + 0.5f) / rows);
    }
}
