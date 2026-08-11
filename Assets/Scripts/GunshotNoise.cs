using System;
using UnityEngine;

public static class GunshotNoise
{
    public static event Action<Vector3, float> OnGunshot;

    public static void Raise(Vector3 position, float radius)
    {
        OnGunshot?.Invoke(position, radius);
    }
}
