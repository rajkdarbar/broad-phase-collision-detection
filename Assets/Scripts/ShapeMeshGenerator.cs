using UnityEngine;

public static class ShapeMeshGenerator
{
    public static Mesh CreateCircle(int segments)
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle = Mathf.Deg2Rad * i * angleStep;
            float x = Mathf.Cos(angle) * 0.5f;
            float y = Mathf.Sin(angle) * 0.5f;

            vertices[i + 1] = new Vector3(x, y, 0f);
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f); // UVs are clamped within [0.25, 0.75]
        }

        // Triangle winding is clockwise here;
        // triangles[baseIdx + 1] and triangles[baseIdx + 2] order might change depending on
        // the front or back face culling
        for (int i = 0; i < segments; i++)
        {
            int baseIdx = i * 3;
            triangles[baseIdx] = 0;
            triangles[baseIdx + 1] = (i + 2 > segments) ? 1 : i + 2;
            triangles[baseIdx + 2] = i + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    public static Mesh CreateTriangle()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0, 0.5f, 0),
        };

        mesh.triangles = new int[] { 0, 2, 1 };

        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 1) };

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }

    public static Mesh CreateHexagon()
    {
        return CreateCircle(6);
    }

    public static Mesh CreateSquare()
    {
        Mesh mesh = new Mesh();

        mesh.vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
        };

        mesh.triangles = new int[] { 2, 1, 0, 0, 3, 2 };

        mesh.uv = new Vector2[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
        };

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        return mesh;
    }
}
