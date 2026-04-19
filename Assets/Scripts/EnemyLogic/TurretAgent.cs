using UnityEngine;

public class TurretAgent : EnemyAgent, IShooter
{
    public override EnemyType Type => EnemyType.Turret;

    public void FacePosition(Vector3 worldPosition) { }
    public void FireAt(Vector3 worldPosition) { }
}
