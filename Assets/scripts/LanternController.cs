
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class LanternController : MonoBehaviour
{
    [Header("Lantern")]
    [SerializeField] private Light2D lanternLight;

    [Header("Lantern Attack")]
    [SerializeField] private float intensityMultiplier = 1.8f;
    [SerializeField] private float radiusMultiplier = 1.4f;
    [SerializeField] private float burstDuration = 0.5f;

    private float normalIntensity;
    private float normalRadius;

    private Coroutine burstRoutine;

    void Awake()
    {
        if (lanternLight == null)
        {
            Debug.LogError("Assign LanternLight!");
            return;
        }

        normalIntensity = lanternLight.intensity;
        normalRadius = lanternLight.pointLightOuterRadius;
    }

    public void LanternBurst()
    {
        if (lanternLight == null)
            return;

        if (burstRoutine != null)
            StopCoroutine(burstRoutine);

        burstRoutine = StartCoroutine(LightBurst());
    }

    IEnumerator LightBurst()
    {
        float elapsed = 0f;

        float brightIntensity =
            normalIntensity * intensityMultiplier;

        float brightRadius =
            normalRadius * radiusMultiplier;

        lanternLight.intensity = brightIntensity;
        lanternLight.pointLightOuterRadius = brightRadius;

        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / burstDuration
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            lanternLight.intensity = Mathf.Lerp(
                brightIntensity, normalIntensity, t
            );

            lanternLight.pointLightOuterRadius = Mathf.Lerp(
                brightRadius, normalRadius, t
            );

            yield return null;
        }

        lanternLight.intensity = normalIntensity;
        lanternLight.pointLightOuterRadius = normalRadius;

        burstRoutine = null;
    }
}
