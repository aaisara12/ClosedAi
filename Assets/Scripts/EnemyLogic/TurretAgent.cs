using UnityEngine;

public class TurretAgent : EnemyAgent, IShooter
{
    public override EnemyType Type => EnemyType.Turret;

    public void FireAt(Vector3 worldPosition) { }
}
