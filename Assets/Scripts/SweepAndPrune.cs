using System.Collections.Generic;
using UnityEngine;

public class SweepAndPrune : MonoBehaviour
{
    public struct Bound
    {
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
        public bool isStatic;

        public Bound(float minX, float maxX, float minY, float maxY, bool isStatic)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            this.isStatic = isStatic;
        }
    }

    public List<ShapeInstance> staticShapes;
    public List<ShapeInstance> dynamicShapes;

    private readonly List<Bound> bounds = new List<Bound>();

    public int collisionCount;
    public bool showGizmos = false;

    void LateUpdate()
    {
        if (dynamicShapes == null || staticShapes == null)
            return;

        collisionCount = DetectCollisions(staticShapes, dynamicShapes);
    }

    private static int CompareByMinX(Bound a, Bound b)
    {
        return a.minX.CompareTo(b.minX);
    }

    public int DetectCollisions(List<ShapeInstance> staticList, List<ShapeInstance> dynamicList)
    {
        bounds.Clear();

        foreach (var s in staticList)
        {
            float half = s.scale * 0.5f;
            bounds.Add(
                new Bound(
                    s.position.x - half,
                    s.position.x + half,
                    s.position.y - half,
                    s.position.y + half,
                    isStatic: true
                )
            );
        }

        foreach (var s in dynamicList)
        {
            float half = s.scale * 0.5f;
            bounds.Add(
                new Bound(
                    s.position.x - half,
                    s.position.x + half,
                    s.position.y - half,
                    s.position.y + half,
                    isStatic: false
                )
            );
        }

        bounds.Sort(CompareByMinX);

        int count = 0;
        for (int i = 0; i < bounds.Count; i++)
        {
            var a = bounds[i];

            if (a.isStatic)
                continue; // skip static objects as they don’t move and don’t initiate collision checks

            for (int j = i + 1; j < bounds.Count; j++)
            {
                var b = bounds[j];

                if (b.minX > a.maxX)
                    break;

                bool yOverlap = (a.maxY >= b.minY) && (b.maxY >= a.minY); // Y overlap check

                if (yOverlap)
                    count++;
            }
        }

        return count;
    }

    // Draw gizmos to visualize the bounds of the shapes.
    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        if (dynamicShapes != null)
        {
            Gizmos.color = Color.red;
            foreach (var s in dynamicShapes)
            {
                float half = s.scale * 0.5f;
                Vector3 center = new Vector3(s.position.x, s.position.y, 0);
                Vector3 size = new Vector3(half * 2, half * 2, 0);
                Gizmos.DrawWireCube(center, size);
            }
        }

        if (staticShapes != null)
        {
            Gizmos.color = Color.green;
            foreach (var s in staticShapes)
            {
                float half = s.scale * 0.5f;
                Vector3 center = new Vector3(s.position.x, s.position.y, 0);
                Vector3 size = new Vector3(half * 2, half * 2, 0);
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}