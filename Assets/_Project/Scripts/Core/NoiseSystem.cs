using System;
using UnityEngine;

public static class NoiseSystem
{
    public static event Action<Vector3, float, GameObject> NoiseEmitted;

    public static void Emit(Vector3 position, float radius, GameObject source)
    {
        if (radius <= 0f) return;
        NoiseEmitted?.Invoke(position, radius, source);
    }
}
