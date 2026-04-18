using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SmokeProjectile : MonoBehaviour
{
    [SerializeField] private GameObject _smokeAreaPrefab;

    private bool _detonated;

    private void OnCollisionEnter(Collision collision)
    {
        if (_detonated) return;
        _detonated = true;

        if (_smokeAreaPrefab != null)
            Instantiate(_smokeAreaPrefab, collision.contacts[0].point, Quaternion.identity);

        Destroy(gameObject);
    }
}
