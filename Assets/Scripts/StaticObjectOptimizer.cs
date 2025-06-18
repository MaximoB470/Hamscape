using UnityEngine;

public class StaticObjectOptimizer : MonoBehaviour
{
    void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        sr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.receiveShadows = false;
    }
}

