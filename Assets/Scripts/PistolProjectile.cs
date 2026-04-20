
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
        Debug.Log($"Projectile hit: {collision.gameObject.name}");
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            collision.collider.GetComponentInChildren<SignalManager>()?.DisconnectFromAll();
        }
    }

    private void OnProjectileHit(Collider collision)
    {
        Debug.Log($"Projectile hit: {collision.gameObject.name}");
        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            collision.GetComponent<Collider>().GetComponentInChildren<SignalManager>()?.DisconnectFromAll();
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            return;
        }
        Destroy(gameObject);
    }

    private void OnEnemyProjectileHit(Collider collision)
    {
        Debug.Log($"Enemy projectile hit: {collision.gameObject.name}");
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            collision.gameObject.GetComponent<Health>()?.TakeDamage(1);
        }

        if (collision.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            return;
        }
        Destroy(gameObject);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (gameObject.CompareTag("Enemy")){
            OnEnemyProjectileHit(other);
            return;
        }
        OnProjectileHit(other);
        
    }
}
