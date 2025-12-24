using System.Collections.Generic;
using UnityEngine;

public class ShapeGPUManager : MonoBehaviour
{
    public Material shapeMaterial; // unlit instanced shader

    private Mesh circleMesh;
    private Mesh triangleMesh;
    private Mesh hexagonMesh;
    private Mesh squareMesh;

    public int circleCount = 10;
    public int triangleCount = 10;
    public int hexagonCount = 10;
    public int squareCount = 10;

    private int shapeId = 0;
    private List<ShapeInstance> circles = new();
    private List<ShapeInstance> triangles = new();
    private List<ShapeInstance> hexagons = new();
    private List<ShapeInstance> squares = new();

    public List<ShapeInstance> staticShapes = new();
    public List<ShapeInstance> dynamicShapes = new();

    private Matrix4x4[] matrixBuffer = new Matrix4x4[1023];
    private MaterialPropertyBlock mpb;

    private Camera cam;
    private float minX, maxX, minY, maxY;

    public UniformGridCollision gridCollision; // reference to the UniformGridCollision script
    public SpatialHashingCollision spatialHashingCollision; // reference to the SpatialHashingCollision script
    public SweepAndPrune sweepAndPrune; // reference to the SweepAndPrune script

    void Awake()
    {
        mpb = new MaterialPropertyBlock();

        cam = Camera.main;
        UpdateScreenBounds();

        // Procedural mesh generation
        circleMesh = ShapeMeshGenerator.CreateCircle(24);
        triangleMesh = ShapeMeshGenerator.CreateTriangle();
        hexagonMesh = ShapeMeshGenerator.CreateHexagon();
        squareMesh = ShapeMeshGenerator.CreateSquare();

        SpawnShapes(circles, circleCount);
        SpawnShapes(triangles, triangleCount);
        SpawnShapes(hexagons, hexagonCount);
        SpawnShapes(squares, squareCount, 0.3f, 0.8f); // larger scale range

        float dt = Time.deltaTime;
        UpdateInstances(squares, dt);
        RenderInstances(squares, squareMesh);

        // Set static shapes once
        staticShapes.Clear();
        staticShapes.AddRange(squares);

        if (sweepAndPrune != null)
            sweepAndPrune.staticShapes = staticShapes;

        if (gridCollision != null)
            gridCollision.staticShapes = staticShapes;

        if (spatialHashingCollision != null)
            spatialHashingCollision.staticShapes = staticShapes;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        UpdateInstances(circles, dt);
        UpdateInstances(triangles, dt);
        UpdateInstances(hexagons, dt);

        RenderInstances(circles, circleMesh);
        RenderInstances(triangles, triangleMesh);
        RenderInstances(hexagons, hexagonMesh);
        RenderInstances(squares, squareMesh);

        // Reuse dynamicShapes list
        dynamicShapes.Clear();
        dynamicShapes.AddRange(circles);
        dynamicShapes.AddRange(triangles);
        dynamicShapes.AddRange(hexagons);

        if (sweepAndPrune != null)
            sweepAndPrune.dynamicShapes = dynamicShapes;

        if (gridCollision != null)
            gridCollision.dynamicShapes = dynamicShapes;

        if (spatialHashingCollision != null)
            spatialHashingCollision.dynamicShapes = dynamicShapes;
    }

    void UpdateScreenBounds()
    {
        Vector3 bl = cam.ScreenToWorldPoint(Vector3.zero);
        Vector3 tr = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        minX = bl.x;
        maxX = tr.x;
        minY = bl.y;
        maxY = tr.y;
    }

    void SpawnShapes(List<ShapeInstance> list, int count, float minScale = 0.1f, float maxScale = 0.4f)
    {
        for (int i = 0; i < count; i++)
        {
            ShapeInstance instance = new ShapeInstance
            {
                id = shapeId++,
                position = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY)),
                rotation = Random.Range(0f, 360f),
                scale = Random.Range(minScale, maxScale),
                velocity = Random.insideUnitCircle * 2f,
                angularSpeed = Random.Range(-90f, 90f),
                color = Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f),
            };
            list.Add(instance);
        }
    }

    void UpdateInstances(List<ShapeInstance> list, float dt)
    {
        for (int i = 0; i < list.Count; i++)
        {
            var shape = list[i];
            shape.position += shape.velocity * dt;
            shape.rotation += shape.angularSpeed * dt;

            // Bounce if outside screen bounds
            float halfSize = shape.scale * 0.5f;

            if (shape.position.x < minX + halfSize || shape.position.x > maxX - halfSize)
                shape.velocity.x *= -1;

            if (shape.position.y < minY + halfSize || shape.position.y > maxY - halfSize)
                shape.velocity.y *= -1;

            list[i] = shape;
        }
    }

    void RenderInstances(List<ShapeInstance> list, Mesh mesh)
    {
        for (int i = 0; i < list.Count; i += 1023)
        {
            int batchSize = Mathf.Min(1023, list.Count - i);

            for (int j = 0; j < batchSize; j++)
            {
                matrixBuffer[j] = list[i + j].GetMatrix();
            }

            Vector4[] colorBuffer = new Vector4[batchSize];
            for (int j = 0; j < batchSize; j++)
            {
                colorBuffer[j] = list[i + j].color;
            }

            mpb.Clear();
            mpb.SetVectorArray("_BaseColor", colorBuffer);

            //Issues one GPU instancing draw call (up to 1023 objects per batch)
            Graphics.DrawMeshInstanced(mesh, 0, shapeMaterial, matrixBuffer, batchSize, mpb);
        }
    }
}
