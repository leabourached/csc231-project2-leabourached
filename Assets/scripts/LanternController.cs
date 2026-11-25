
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
    [SerializeField] private float lanternAttackRange = 3.5f;

    private float normalIntensity;
    private float normalRadius;
    private float normalInnerRadius;

    private int drainStage = 0;

    private Coroutine burstRoutine;
    private PlayerHealth health;

    public int DrainStage => drainStage;

    public bool IsDead =>
        health != null && health.IsDead;

    public float SpeedMultiplier
    {
        get
        {
            switch (drainStage)
            {
                case 1: return 0.75f;
                case 2: return 0.4f;
                case 3: return 0f;
                default: return 1f;
            }
        }
    }

    private float LightMultiplier
    {
        get
        {
            switch (drainStage)
            {
                case 1: return 0.65f;
                case 2: return 0.25f;
                case 3: return 0f;
                default: return 1f;
            }
        }
    }

    void Awake()
    {
        health = GetComponent<PlayerHealth>();

        if (lanternLight == null)
        {
            Debug.LogError("Assign LanternLight!");
            return;
        }

        normalIntensity = lanternLight.intensity;
        normalRadius = lanternLight.pointLightOuterRadius;
        normalInnerRadius = lanternLight.pointLightInnerRadius;
    }

    public void DrainLantern()
    {
        if (IsDead || drainStage >= 3)
            return;

        StopBurst();

        drainStage++;

        ApplyLight(1f, 1f);

        Debug.Log("Lantern drain stage: " + drainStage);

        if (drainStage >= 3)
        {
            Debug.Log("Fern's flame has gone out!");

            if (health != null)
                health.KillFromLantern();
        }
    }

    public void RestoreLantern()
    {
        if (IsDead)
            return;

        StopBurst();

        drainStage = 0;

        ApplyLight(1f, 1f);

        Debug.Log("Lantern and movement restored!");
    }

    public void LanternBurst()
    {
        if (IsDead || drainStage >= 3)
            return;

        StopBurst();

        bool hit = ShadowDrainer.HitNearby(
            this,
            lanternAttackRange
        );

        if (hit)
            RestoreLantern();

        if (lanternLight != null)
            burstRoutine = StartCoroutine(LightBurst());
    }

    private IEnumerator LightBurst()
    {
        float elapsed = 0f;

        ApplyLight(
            intensityMultiplier,
            radiusMultiplier
        );

        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed / burstDuration
            );

            t = Mathf.SmoothStep(0f, 1f, t);

            ApplyLight(
                Mathf.Lerp(intensityMultiplier, 1f, t),
                Mathf.Lerp(radiusMultiplier, 1f, t)
            );

            yield return null;
        }

        ApplyLight(1f, 1f);

        burstRoutine = null;
    }

    private void ApplyLight(
        float intensityBoost,
        float radiusBoost)
    {
        if (lanternLight == null)
            return;

        float multiplier = LightMultiplier;

        lanternLight.intensity =
            normalIntensity * multiplier * intensityBoost;

        float outer =
            normalRadius * multiplier * radiusBoost;

        float inner =
            normalInnerRadius * multiplier * radiusBoost;

        lanternLight.pointLightInnerRadius = 0f;

        lanternLight.pointLightOuterRadius = outer;

        lanternLight.pointLightInnerRadius =
            Mathf.Min(inner, outer);
    }

    private void StopBurst()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            burstRoutine = null;
        }
    }

    public float GetPullVelocity()
    {
        return ShadowDrainer.GetPullVelocity(this);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.65f, 0f);

        Gizmos.DrawWireSphere(
            transform.position,
            lanternAttackRange
        );
    }
}
