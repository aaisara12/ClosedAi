using UnityEngine;

public interface IMovable
{
    void MoveTo(Vector3 position);
    void Stop();
    bool HasReached { get; }
}

public interface IShooter
{
    void FireAt(Vector3 worldPosition);
    void FacePosition(Vector3 worldPosition);
}

public interface IShieldProvider
{
    bool canGiveShield();
    void GiveShield(EnemyAgent target);
    void RemoveShield(EnemyAgent target);
}
