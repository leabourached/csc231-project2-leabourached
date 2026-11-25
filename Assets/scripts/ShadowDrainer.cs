
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShadowDrainer : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private LanternController player;

    [Header("Detection")]
    [SerializeField] private float emergenceRange = 4.5f;
    [SerializeField] private float drainRange = 3f;

    [Header("Pulling")]
    [SerializeField] private float pullStrength = 2.5f;
    [SerializeField] private float stopDistance = 0.6f;

    [Header("Energy Drain")]
    [SerializeField] private float firstDrainDelay = 1f;
    [SerializeField] private float drainInterval = 2.5f;

    private Animator anim;
    private SpriteRenderer[] sprites;
    private Collider2D[] colliders;

    private float drainTimer;

    private enum State
    {
        Hidden,
        Emerging,
        Draining,
        Dead
    }

    private State state = State.Hidden;

    private static readonly List<ShadowDrainer> monsters =
        new List<ShadowDrainer>();

    void OnEnable()
    {
        monsters.Add(this);
    }

    void OnDisable()
    {
        monsters.Remove(this);
    }

    void Awake()
    {
        anim = GetComponentInChildren<Animator>(true);
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        colliders = GetComponentsInChildren<Collider2D>(true);

        if (anim != null)
        {
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.enabled = false;
        }

        foreach (Collider2D col in colliders)
        {
            col.isTrigger = true;
            col.enabled = false;
        }

        SetVisible(false);
    }

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<LanternController>();

        if (player == null)
            Debug.LogError("Drainer cannot find Fern!");
    }

    void Update()
    {
        if (player == null || player.IsDead ||
            state == State.Dead)
            return;

        float distance = Vector2.Distance(
            transform.position,
            player.transform.position
        );

        if (state == State.Hidden &&
            distance <= emergenceRange)
        {
            StartCoroutine(Emerge());
        }

        if (state != State.Draining)
            return;

        if (FindClosestDrainer(player) != this)
        {
            drainTimer = firstDrainDelay;
            return;
        }

        drainTimer -= Time.deltaTime;

        if (drainTimer <= 0f)
        {
            player.DrainLantern();
            drainTimer = drainInterval;
        }
    }

    private IEnumerator Emerge()
    {
        state = State.Emerging;

        if (anim != null)
            anim.enabled = true;

        SetVisible(true);

        foreach (Collider2D col in colliders)
            col.enabled = true;

        Debug.Log("Shadow drainer emerging!");

        if (PlayAnimation("drainer_emerge"))
            yield return WaitForAnimation("drainer_emerge");

        state = State.Draining;
        drainTimer = firstDrainDelay;

        PlayAnimation("drainer_drain");
    }

    private static ShadowDrainer FindClosestDrainer(
        LanternController target)
    {
        if (target == null || target.IsDead)
            return null;

        ShadowDrainer closest = null;
        float closestDistance = float.PositiveInfinity;

        foreach (ShadowDrainer monster in monsters)
        {
            if (monster == null ||
                monster.state != State.Draining ||
                monster.player != target)
                continue;

            float distance = Vector2.Distance(
                monster.transform.position,
                target.transform.position
            );

            if (distance <= monster.drainRange &&
                distance < closestDistance)
            {
                closest = monster;
                closestDistance = distance;
            }
        }

        return closest;
    }

    public static float GetPullVelocity(
        LanternController target)
    {
        ShadowDrainer monster = FindClosestDrainer(target);

        if (monster == null)
            return 0f;

        float difference =
            monster.transform.position.x -
            target.transform.position.x;

        float remainingDistance =
            Mathf.Abs(difference) - monster.stopDistance;

        if (remainingDistance <= 0f)
            return 0f;

        float strength = Mathf.Min(
            monster.pullStrength,
            remainingDistance / Time.fixedDeltaTime
        );

        return Mathf.Sign(difference) * strength;
    }

    public static bool HitNearby(
        LanternController target, float radius)
    {
        bool hit = false;

        foreach (ShadowDrainer monster in monsters)
        {
            if (monster == null ||
                monster.player != target ||
                monster.state == State.Hidden ||
                monster.state == State.Dead)
                continue;

            float distance = Vector2.Distance(
                monster.transform.position,
                target.transform.position
            );

            if (distance <= radius)
            {
                monster.Defeat();
                hit = true;
            }
        }

        return hit;
    }

    private void Defeat()
    {
        if (state == State.Dead)
            return;

        StopAllCoroutines();

        state = State.Dead;

        foreach (Collider2D col in colliders)
            col.enabled = false;

        Debug.Log("Shadow drainer defeated!");

        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (PlayAnimation("drainer_death"))
            yield return WaitForAnimation("drainer_death");

        Destroy(gameObject);
    }

    private bool PlayAnimation(string stateName)
    {
        if (anim == null)
            return false;

        int hash = Animator.StringToHash(
            "Base Layer." + stateName
        );

        if (!anim.HasState(0, hash))
        {
            Debug.LogError("Missing animation: " + stateName);
            return false;
        }

        anim.Play(hash, 0, 0f);
        anim.Update(0f);

        return true;
    }

    private IEnumerator WaitForAnimation(string stateName)
    {
        float elapsed = 0f;

        yield return null;

        while (elapsed < 10f)
        {
            AnimatorStateInfo info =
                anim.GetCurrentAnimatorStateInfo(0);

            if (info.IsName("Base Layer." + stateName) &&
                info.normalizedTime >= 1f)
                yield break;

            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private void SetVisible(bool visible)
    {
        foreach (SpriteRenderer sprite in sprites)
            sprite.enabled = visible;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            emergenceRange
        );

        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            transform.position,
            drainRange
        );
    }
}
