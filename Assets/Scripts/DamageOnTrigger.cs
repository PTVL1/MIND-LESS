using UnityEngine;

public class DamageOnTrigger : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private bool destroyAfterHit;

    private void OnTriggerEnter(Collider other)
    {
        MindLess.Gameplay.HealthSystem health = other.GetComponentInParent<MindLess.Gameplay.HealthSystem>();
        if (health == null)
        {
            return;
        }

        health.TakeDamage(damage);

        if (destroyAfterHit)
        {
            Destroy(gameObject);
        }
    }
}
