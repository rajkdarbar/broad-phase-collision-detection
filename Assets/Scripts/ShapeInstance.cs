using UnityEngine;

public struct ShapeInstance
{
    public int id;
    public Vector2 position;
    public float rotation;
    public float scale;

    public Vector2 velocity;
    public float angularSpeed;

    public Color color;

    // Model matrix (object space → world space transform)
    public Matrix4x4 GetMatrix()
    {
        return Matrix4x4.TRS(
            new Vector3(position.x, position.y, 0f),
            Quaternion.Euler(0f, 0f, rotation),
            Vector3.one * scale
        );
    }
}