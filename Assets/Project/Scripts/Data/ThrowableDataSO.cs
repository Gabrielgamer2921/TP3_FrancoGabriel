using UnityEngine;

[CreateAssetMenu(fileName = "ThrowableData", menuName = "Cowboy FPS/Throwable Data")]
public class ThrowableDataSO : ScriptableObject
{
    [Header("General")]
    public string displayName = "Dinamita";
    public Dynamite prefab;
    public ExplosionDataSO explosion;
    [Min(0)] public int startingCount = 3;
    [Min(1)] public int maxCount = 5;

    [Header("Mecha")]
    [Min(0.1f)] public float fuseTime = 3.5f;
    public bool canCook = true;

    [Header("Lanzamiento")]
    [Min(0f)] public float throwForce = 16f;
    [Min(0f)] public float upwardForce = 4f;
    [Min(0f)] public float throwCooldown = 0.8f;
    [Min(0f)] public float holsterTime = 0.35f;
}
