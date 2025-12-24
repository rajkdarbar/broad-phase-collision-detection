using System.Collections.Generic;
using UnityEngine;

public class SpatialHashingCollision : MonoBehaviour
{
    public List<ShapeInstance> staticShapes;
    public List<ShapeInstance> dynamicShapes;

    public float cellSize = 1.5f;
    private readonly List<Vector2Int> cells = new List<Vector2Int>();

    private readonly Dictionary<Vector2Int, List<ShapeInstance>> hashGrid =
        new Dictionary<Vector2Int, List<ShapeInstance>>();

    private readonly HashSet<(int, int)> seenIds = new HashSet<(int, int)>();
    private readonly HashSet<int> staticIds = new HashSet<int>();

    public int collisionCount;
    public bool drawGizmos = false;


    void Start()
    {
        var mgr = Object.FindFirstObjectByType<ShapeGPUManager>();
        if (mgr == null)
        {
            Debug.LogError("No ShapeGPUManager found in scene");
            return;
        }
        staticShapes = mgr.staticShapes;
        dynamicShapes = mgr.dynamicShapes;

        staticIds.Clear();
        foreach (var s in staticShapes)
            staticIds.Add(s.id);
    }

    void LateUpdate()
    {
        if (staticShapes == null || dynamicShapes == null)
        {
            Debug.LogWarning("Shape lists not assigned");
            return;
        }

        collisionCount = DetectCollisions();
    }

    int DetectCollisions()
    {
        int count = 0;

        // Clear spatial hash and seen set
        hashGrid.Clear();
        seenIds.Clear();

        // Insert all shapes into the hash
        foreach (var sh in staticShapes)
            InsertToHash(sh);

        foreach (var sh in dynamicShapes)
            InsertToHash(sh);

        // Scan only the nonempty buckets        
        foreach (var bucket in hashGrid.Values)
        {
            int n = bucket.Count;
            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    var a = bucket[i];
                    var b = bucket[j];

                    // Skip static–static pairs
                    if (staticIds.Contains(a.id) && staticIds.Contains(b.id))
                        continue;

                    if (a.id > b.id)
                        (a, b) = (b, a);

                    var key = (a.id, b.id);
                    if (!seenIds.Add(key))
                        continue; // already tested this unordered pair

                    // Precise overlap test
                    if (IsOverlapping(a, b))
                        count++;
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
        int x = Mathf.FloorToInt(pos.x / cellSize);
        int y = Mathf.FloorToInt(pos.y / cellSize);
        return new Vector2Int(x, y);
    }

    void InsertToHash(ShapeInstance s)
    {
        // Determine which cells this shape covers
        cells.Clear();

        AABB box = GetAABB(s);

        // Get the min and max cell indices covered by the shape
        Vector2Int cMin = WorldToCell(box.min); // the cell of the AABB’s bottom-left corner
        Vector2Int cMax = WorldToCell(box.max); // the cell of the AABB’s top-right corner

        for (int x = cMin.x; x <= cMax.x; x++)
            for (int y = cMin.y; y <= cMax.y; y++)
                cells.Add(new Vector2Int(x, y));

        // Insert into each hashed bucket
        foreach (var c in cells)
        {
            if (!hashGrid.TryGetValue(c, out var list))
            {
                list = new List<ShapeInstance>();
                hashGrid[c] = list;
            }
            
            list.Add(s);
        }
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

    void OnDrawGizmos()
    {
        if (!drawGizmos)
            return;

        Gizmos.color = Color.cyan;
        foreach (var kv in hashGrid)
        {
            Vector3 center = new Vector3(kv.Key.x + 0.5f, kv.Key.y + 0.5f, 0) * cellSize;
            Gizmos.DrawWireCube(center, Vector3.one * cellSize);
        }
    }
}
