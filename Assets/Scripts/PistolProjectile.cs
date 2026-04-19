using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PistolProjectile : MonoBehaviour
{
    [SerializeField] private float _lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, _lifetime);

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnProjectileHit(collision);
        Destroy(gameObject);
    }

    private void OnProjectileHit(Collision collision)
    {
        // Add hit effects, damage, etc. here
    }

    private void OnEnemyProjectileHit(Collider other)
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        OnEnemyProjectileHit(other);
        Destroy(gameObject);
    }
}
