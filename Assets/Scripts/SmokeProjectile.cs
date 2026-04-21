using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SmokeProjectile : MonoBehaviour
{
    [SerializeField] private GameObject _smokeAreaPrefab;

    private bool _detonated;

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            return;
        if (_detonated) return;
        _detonated = true;

        if (_smokeAreaPrefab != null)
            Instantiate(_smokeAreaPrefab, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
