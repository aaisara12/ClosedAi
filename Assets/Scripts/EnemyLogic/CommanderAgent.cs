using UnityEngine;
using System.Collections;

[RequireComponent(typeof(EnemyNavigator))]
public class CommanderAgent : EnemyAgent, IMovable, IShooter, IShieldProvider
{
    public override EnemyType Type => EnemyType.Commander;
    [SerializeField] private float shieldCD = 5f;

    private bool _shieldAvailable = true;

    IEnumerator ShieldCoroutine(EnemyAgent target) {
        _shieldAvailable = false;

        var targetHealth = target.GetComponent<Health>();
        if (targetHealth != null)
        {
            targetHealth.hasShield = true;
        }

        yield return new WaitForSeconds(shieldCD);
        _shieldAvailable = true; 
    }

    private EnemyNavigator _nav;

    protected override void Awake()
    {
        base.Awake();
        _nav = GetComponent<EnemyNavigator>();
    }

    public void MoveTo(Vector3 position) => _nav.MoveTo(position);
    public void Stop() => _nav.Stop();
    public bool HasReached => _nav.HasReached;

    public void FacePosition(Vector3 worldPosition) { }
    public void FireAt(Vector3 worldPosition) { }

    public bool canGiveShield() { return !_shieldAvailable; }
    public void GiveShield(EnemyAgent target)
    {
        if (_shieldAvailable)
        {
            StartCoroutine(ShieldCoroutine(target));
        }
    }
    public void RemoveShield(EnemyAgent target) { }
}
