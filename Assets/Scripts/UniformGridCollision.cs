using System.Collections.Generic;
using UnityEngine;

public class UniformGridCollision : MonoBehaviour
{
    private Camera cam;
    private float minX, maxX, minY, maxY;

    public List<ShapeInstance> staticShapes;
    public List<ShapeInstance> dynamicShapes;

    public float width = 0;
    public float height = 0;
    public float cellSize = 1.5f;

    private int numRows; // number of rows (cells along Y) 
    private int numCols; // number of columns (cells along X)    
    private List<ShapeInstance>[,] grid; // grid[row, col]

    public int collisionCount;

    private readonly List<Vector2Int> cells = new List<Vector2Int>();
    private readonly HashSet<(int, int)> seenIds = new HashSet<(int, int)>();
    private readonly HashSet<int> staticIds = new HashSet<int>();

    public bool showGizmos = false;

    void Start()
    {
        var mgr = Object.FindFirstObjectByType<ShapeGPUManager>();
        if (mgr != null)
        {
            staticShapes = mgr.staticShapes;
            dynamicShapes = mgr.dynamicShapes;

            staticIds.Clear();
            foreach (var s in staticShapes)
                staticIds.Add(s.id);
        }
        else
        {
            Debug.LogError("No ShapeGPUManager found in scene");
        }

        cam = Camera.main;
        UpdateGridMetrics();
    }

    void LateUpdate()
    {
        if (staticShapes == null || dynamicShapes == null)
        {
            Debug.LogWarning("Shape lists not assigned");
            return;
        }

        UpdateGridMetrics();
        collisionCount = DetectCollisions();

        if (showGizmos)
            DrawGridLines();
    }

    void UpdateGridMetrics()
    {
        Vector3 bl = cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 tr = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cam.nearClipPlane));

        minX = bl.x;
        maxX = tr.x;
        minY = bl.y;
        maxY = tr.y;

        width = maxX - minX;
        height = maxY - minY;

        int rows = Mathf.CeilToInt(height / cellSize);
        int cols = Mathf.CeilToInt(width / cellSize);

        if (rows != numRows || cols != numCols)
        {
            numRows = rows;
            numCols = cols;

            grid = new List<ShapeInstance>[numRows, numCols];
            for (int row = 0; row < numRows; row++)
                for (int col = 0; col < numCols; col++)
                    grid[row, col] = new List<ShapeInstance>();
        }
    }

    int DetectCollisions()
    {
        int count = 0;

        if (grid == null)
            return 0;

        // Clear every bucket
        for (int row = 0; row < numRows; row++)
            for (int col = 0; col < numCols; col++)
                grid[row, col].Clear();

        // Insert all shapes
        foreach (var s in staticShapes)
            Insert(s);

        foreach (var s in dynamicShapes)
            Insert(s);

        // Scan each cell
        seenIds.Clear();

        for (int row = 0; row < numRows; row++)
        {
            for (int col = 0; col < numCols; col++)
            {
                var bucket = grid[row, col];
                int n = bucket.Count;
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        var a = bucket[i];
                        var b = bucket[j];

                        // Skip static–static
                        if (staticIds.Contains(a.id) && staticIds.Contains(b.id))
                            continue;

                        if (a.id > b.id)
                            (a, b) = (b, a);

                        var key = (a.id, b.id);
                        if (!seenIds.Add(key))
                            continue;

                        if (IsOverlapping(a, b))
                            count++;
                    }
                }
            }
        }

        return count;
    }


    AABB GetAABB(ShapeInstance s)
    {
        Vector2 half = Vector2.one * (s.scale * 0.5f);

        AABB box;
        box.min = s.position - half;
        box.max = s.position + half;

        return box;
    }

    Vector2Int WorldToCell(Vector2 pos)
    {
        // Convert world coordinates → grid indices
        // (pos - min) shifts world coordinates so the bottom-left of the camera becomes (0,0)
        int row = Mathf.FloorToInt((pos.y - minY) / cellSize);
        int col = Mathf.FloorToInt((pos.x - minX) / cellSize);

        row = Mathf.Clamp(row, 0, numRows - 1);
        col = Mathf.Clamp(col, 0, numCols - 1);

        // Return as (row, col)
        return new Vector2Int(row, col);
    }

    public List<Vector2Int> CoveredCells(ShapeInstance s)
    {
        cells.Clear();

        AABB box = GetAABB(s);

        // Get the min and max cell indices covered by the shape
        Vector2Int cMin = WorldToCell(box.min); // the cell of the AABB’s bottom-left corner
        Vector2Int cMax = WorldToCell(box.max); // the cell of the AABB’s top-right corner

        // Traverse row-major: row first, then column
        for (int row = cMin.x; row <= cMax.x; row++)
            for (int col = cMin.y; col <= cMax.y; col++)
                cells.Add(new Vector2Int(row, col)); // includes every cell in that rectangular region defined by cMin and cMax

        return cells;
    }

    void Insert(ShapeInstance s)
    {
        var coveredCells = CoveredCells(s);

        foreach (var cell in coveredCells)
            grid[cell.x, cell.y].Add(s); // [row, col]
    }

    bool Overlaps(AABB a, AABB b)
    {
        return a.min.x <= b.max.x && a.max.x >= b.min.x &&
               a.min.y <= b.max.y && a.max.y >= b.min.y;
    }

    bool IsOverlapping(ShapeInstance a, ShapeInstance b)
    {
        return Overlaps(GetAABB(a), GetAABB(b));
    }

    void DrawGridLines()
    {
        // Vertical grid lines
        for (float x = minX; x <= maxX; x += cellSize)
        {
            Debug.DrawLine(new Vector3(x, minY, 0f), new Vector3(x, maxY, 0f), Color.green);
        }

        // Horizontal grid lines
        for (float y = minY; y <= maxY; y += cellSize)
        {
            Debug.DrawLine(new Vector3(minX, y, 0f), new Vector3(maxX, y, 0f), Color.green);
        }
    }
}
