using UnityEngine;
using System.Collections;

public class CollisionFPSProfiler : MonoBehaviour
{
    public UniformGridCollision uniformGridCollision;
    public SpatialHashingCollision spatialHashingCollision;
    public SweepAndPrune sweepAndPrune;

    [Space(10)]
    public float testDuration = 5f;

    [Space(10)]
    [Header("Profiling Results")]
    public float uniformGridFPS;
    public float spatialHashingFPS;
    public float sweepAndPruneFPS;

    enum Mode { Grid, Hash, SAP }
    Mode mode = Mode.Grid;

    int frames;
    float elapsed;

    IEnumerator Start()
    {
        uniformGridCollision.enabled = false;
        spatialHashingCollision.enabled = false;
        sweepAndPrune.enabled = false;

        while (true)
        {
            uniformGridCollision.enabled = (mode == Mode.Grid);
            spatialHashingCollision.enabled = (mode == Mode.Hash);
            sweepAndPrune.enabled = (mode == Mode.SAP);

            frames = 0;
            elapsed = 0f;
            yield return null; // wait exactly one frame

            while (elapsed < testDuration)
            {
                yield return null;
                frames++;
                elapsed += Time.unscaledDeltaTime;
            }

            float avg = frames / elapsed;
            switch (mode)
            {
                case Mode.Grid: uniformGridFPS = avg; break;
                case Mode.Hash: spatialHashingFPS = avg; break;
                case Mode.SAP: sweepAndPruneFPS = avg; break;
            }

            mode = (Mode)(((int)mode + 1) % 3);
        }
    }
}
