using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CoveringFireStrategy : Strategy
{
    public override int Priority => 11;
    public override StrategyRequirement[] Requirements => new[]
    {
        new StrategyRequirement { Type = EnemyType.Melee,  Count = 1 },
        new StrategyRequirement { Type = EnemyType.Ranged, Count = 3 },
    };

    private const float AimDistance  = 4f;
    private const float SpreadRadius = 1.5f;

    private EnemyAgent       _melee;
    private List<EnemyAgent> _ranged;
    private Vector3          _prevMeleePos;
    private Vector3          _meleeVelocity;
    private int              _terrainMask;

    public override void OnStart()
    {
        Debug.Log("Starting covering fire strategy");

        _melee        = _agents.First(a => a.Type == EnemyType.Melee);
        _ranged       = _agents.Where(a => a.Type == EnemyType.Ranged).ToList();
        _prevMeleePos = _melee.transform.position;
        _terrainMask  = LayerMask.GetMask("Default", "Terrain");
    }

    public override void Tick(Vector3 playerPos, bool playerSpotted)
    {
        Vector3 meleePos = _melee.transform.position;

        // Track melee velocity for aim direction
        _meleeVelocity = (meleePos - _prevMeleePos) / Mathf.Max(Time.deltaTime, 0.001f);
        _prevMeleePos  = meleePos;

        (_melee as IMovable)?.MoveTo(playerPos);

        // Aim direction: along melee velocity, falling back to melee→player
        Vector3 aimDir = _meleeVelocity.sqrMagnitude > 0.5f
            ? _meleeVelocity.normalized
            : (playerPos - meleePos).normalized;

        foreach (var agent in _ranged)
        {
            var movable = agent as IMovable;
            var shooter  = agent as IShooter;

            // Check for clear path from ranged agent toward the melee unit's area
            if (HasClearPath(agent.transform.position, meleePos))
            {
                Vector2 spread    = Random.insideUnitCircle * SpreadRadius;
                Vector3 aimTarget = meleePos + aimDir * AimDistance + new Vector3(spread.x, 0f, spread.y);
                movable?.Stop();
                shooter?.FacePosition(aimTarget);
                shooter?.FireAt(aimTarget);
            }
            else
            {
                movable?.MoveTo(meleePos);
            }
        }
    }

    // Returns true when no solid terrain blocks the line from ranged to melee area.
    private bool HasClearPath(Vector3 from, Vector3 to)
    {
        Vector3 origin = from + Vector3.up * 1.5f;
        Vector3 dir    = to - origin;
        return !Physics.Raycast(origin, dir.normalized, dir.magnitude, _terrainMask);
    }

    public override void End()
    {
        foreach (var a in _agents)
            (a as IMovable)?.Stop();
    }
}
