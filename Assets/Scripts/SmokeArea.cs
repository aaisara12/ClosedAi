using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class SmokeArea : MonoBehaviour
{
    [SerializeField] private float _radius = 5f;
    [SerializeField] private float _duration = 10f;

    private void Awake()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, _duration);
    }

    private void OnTriggerEnter(Collider other)
    {
        OnEntityEntered(other);
    }

    private void OnTriggerExit(Collider other)
    {
        OnEntityExited(other);
    }

    private void OnEntityEntered(Collider col)
    {
        if (col.CompareTag("Player"))
            AudioSystem.SetLowpass(true);
    }

    private void OnEntityExited(Collider col)
    {
        if (col.CompareTag("Player"))
            AudioSystem.SetLowpass(false);
    }
}
