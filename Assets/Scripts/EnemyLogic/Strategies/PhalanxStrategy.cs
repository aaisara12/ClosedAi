using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PhalanxStrategy : Strategy
{
    public override int Priority => 14;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Melee,  Count = 3 },
        new StrategyRequirement { Type = EnemyType.Ranged, Count = 1 },
    };

    private const float OrbitRadius = 3f;
    private const float OrbitSpeed  = 45f; // degrees per second
    private const float ChaseRange  = 4f;
    private const float MaxLeash    = 8f;

    private EnemyAgent _ranged;
    private List<EnemyAgent> _melee;
    private float[] _orbitOffsets;
    private bool[]  _chasing;
    private float   _orbitAngle;
    private int     _losMask;

    public override void OnStart()
    {
        Debug.Log("Starting Phalanx strategy");

        foreach (var a in _agents)
            a.GetComponentInChildren<SignalStatus>()?.SetIcon(SignalIcon.Square);

        _ranged = _agents.First(a => a.Type == EnemyType.Ranged);
        _melee  = _agents.Where(a => a.Type == EnemyType.Melee).ToList();

        _orbitOffsets = new float[_melee.Count];
        _chasing      = new bool[_melee.Count];
        for (int i = 0; i < _melee.Count; i++)
            _orbitOffsets[i] = 360f * i / _melee.Count;

        _losMask = ~LayerMask.GetMask("Enemy");
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        _orbitAngle += OrbitSpeed * Time.deltaTime;

        var rangedMovable = _ranged as IMovable;
        var rangedShooter = _ranged as IShooter;
        if (HasLOS(_ranged.transform.position, playerPos))
        {
            rangedMovable?.Stop();
            rangedShooter?.FacePosition(playerPos);
            rangedShooter?.FireAt(playerPos);
        }
        else
        {
            rangedMovable?.MoveTo(playerPos);
        }

        Vector3 rangedPos = _ranged.transform.position;
        for (int i = 0; i < _melee.Count; i++)
        {
            EnemyAgent m = _melee[i];
            var movable  = m as IMovable;

            float distToPlayer = Vector3.Distance(m.transform.position, playerPos);
            float distToRanged = Vector3.Distance(m.transform.position, rangedPos);

            if (!_chasing[i] && distToPlayer < ChaseRange) _chasing[i] = true;
            if (_chasing[i]  && distToRanged > MaxLeash)   _chasing[i] = false;

            if (_chasing[i])
            {
                movable?.MoveTo(playerPos);
            }
            else
            {
                float   angle    = _orbitAngle + _orbitOffsets[i];
                Vector3 orbitPos = rangedPos + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * OrbitRadius;
                movable?.MoveTo(orbitPos);
            }
        }
    }

    private bool HasLOS(Vector3 from, Vector3 to)
    {
        Vector3 origin = from + Vector3.up * 1.5f;
        Vector3 dir    = to - origin;
        return Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, _losMask)
               && hit.collider.CompareTag("Player");
    }

    public override void End()
    {
        foreach (var a in _agents)
            a.GetComponentInChildren<SignalStatus>()?.ResetIcon();
        foreach (var a in _agents)
            (a as IMovable)?.Stop();
    }
}
