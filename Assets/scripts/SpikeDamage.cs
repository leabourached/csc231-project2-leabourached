
using UnityEngine;

public class SpikeDamage : MonoBehaviour
{
    [SerializeField] private int damage = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        DamagePlayer(other);
    }

    private void DamagePlayer(Collider2D other)
    {
        PlayerHealth health =
            other.GetComponentInParent<PlayerHealth>();

        if (health != null && !health.IsDead)
        {
            health.TakeDamage(damage);
        }
    }
}
