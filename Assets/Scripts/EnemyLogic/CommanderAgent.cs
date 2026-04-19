using UnityEngine;

[RequireComponent(typeof(EnemyNavigator))]
public class CommanderAgent : EnemyAgent, IMovable, IShooter, IShieldProvider
{
    public override EnemyType Type => EnemyType.Commander;

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

    public bool canGiveShield() { return false; }
    public void GiveShield(EnemyAgent target) { }
    public void RemoveShield(EnemyAgent target) { }
}
