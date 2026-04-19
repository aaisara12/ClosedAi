#if UNITY_EDITOR || DEVELOPMENT_BUILD
#nullable enable

using UnityEngine;
using UnityEngine.InputSystem;

public class DebugDamageTool : MonoBehaviour
{
    [SerializeField] private bool isEnabled = false;
    [SerializeField] private int damageAmount = 25;
    [SerializeField] private float maxDistance = 100f;

    private void Update()
    {
        if (!isEnabled) return;
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (Camera.main == null)
        {
            Debug.LogWarning("[DebugDamageTool] No main camera found.");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            Debug.DrawLine(ray.origin, hit.point, Color.red, 2f);

            Health? health = hit.collider.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(damageAmount);
                Debug.Log($"[DebugDamageTool] Hit '{hit.collider.gameObject.name}' — dealt {damageAmount} damage. Current health: {health.CurrentHealth}");
            }
            else
            {
                Debug.Log($"[DebugDamageTool] Hit '{hit.collider.gameObject.name}' — no Health component found.");
            }
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * maxDistance, Color.yellow, 2f);
            Debug.Log("[DebugDamageTool] Raycast missed — no collider hit.");
        }
    }
}
#endif

