using System.Collections.Generic;
using UnityEngine;

public class UnitConflictPair
{
    public UnitPiece initiator;
    public UnitPiece target;
    public bool directOverlap;
}

public class UnitConflictResult
{
    public UnitPiece initiator;
    public UnitPiece target;

    public bool directOverlap;

    public int initiatorBasePower;
    public int initiatorOwnSupportBonus;
    public int initiatorExternalSupport;
    public int initiatorTotalPower;

    public int targetBasePower;
    public int targetExternalSupport;
    public int targetTotalPower;

    public bool initiatorDefeated;
    public bool targetDefeated;
}

public static class UnitConflictResolver
{
    public static List<UnitConflictPair> BuildConflicts(
        TriangleBoard board,
        IReadOnlyList<UnitPiece> newlyPlacedUnits
    )
    {
        List<UnitConflictPair> result = new();

        if (board == null || newlyPlacedUnits == null)
            return result;

        foreach (UnitPiece initiator in newlyPlacedUnits)
        {
            if (!CanFight(initiator))
                continue;

            foreach (UnitPiece enemy in board.Units)
            {
                if (!CanFight(enemy))
                    continue;

                if (enemy == initiator)
                    continue;

                if (enemy.Owner == initiator.Owner)
                    continue;

                bool directOverlap = CellsOverlap(
                    initiator.OccupiedCells,
                    enemy.OccupiedCells
                );

                bool inConflictRange = CellsTouchOrOverlap(
                    enemy.OccupiedCells,
                    initiator.SupportCells
                );

                if (!directOverlap && !inConflictRange)
                    continue;

                result.Add(new UnitConflictPair
                {
                    initiator = initiator,
                    target = enemy,
                    directOverlap = directOverlap
                });

                Debug.Log(
                    $"Checking conflict: {initiator.name} vs {enemy.name} | " +
                    $"Direct overlap: {directOverlap} | " +
                    $"In support/conflict range: {inConflictRange} | " +
                    $"Initiator support cells: {initiator.SupportCells.Count} | " +
                    $"Enemy base cells: {enemy.OccupiedCells.Count}"
                );
            }
        }

        return result;
    }

    public static List<UnitConflictResult> ResolveConflicts(
        TriangleBoard board,
        IReadOnlyList<UnitConflictPair> conflicts
    )
    {
        List<UnitConflictResult> results = new();

        if (board == null || conflicts == null)
            return results;

        foreach (UnitConflictPair conflict in conflicts)
        {
            if (conflict == null)
                continue;

            UnitPiece initiator = conflict.initiator;
            UnitPiece target = conflict.target;

            if (!CanFight(initiator) || !CanFight(target))
                continue;

            int initiatorPower = GetPower(initiator);
            int initiatorOwnSupportBonus = conflict.directOverlap ? GetSupport(initiator) : 0;
            int initiatorExternalSupport = GetExternalSupportPower(board, initiator);

            int targetPower = GetPower(target);
            int targetExternalSupport = GetExternalSupportPower(board, target);

            int initiatorTotal =
                initiatorPower +
                initiatorOwnSupportBonus +
                initiatorExternalSupport;

            int targetTotal =
                targetPower +
                targetExternalSupport;

            UnitConflictResult result = new UnitConflictResult
            {
                initiator = initiator,
                target = target,

                directOverlap = conflict.directOverlap,

                initiatorBasePower = initiatorPower,
                initiatorOwnSupportBonus = initiatorOwnSupportBonus,
                initiatorExternalSupport = initiatorExternalSupport,
                initiatorTotalPower = initiatorTotal,

                targetBasePower = targetPower,
                targetExternalSupport = targetExternalSupport,
                targetTotalPower = targetTotal
            };

            if (initiatorTotal > targetTotal)
            {
                result.targetDefeated = true;
            }
            else if (targetTotal > initiatorTotal)
            {
                result.initiatorDefeated = true;
            }
            else
            {
                result.initiatorDefeated = true;
                result.targetDefeated = true;
            }

            results.Add(result);
        }

        return results;
    }

    public static int GetExternalSupportPower(TriangleBoard board, UnitPiece receiver)
    {
        return UnitSupportUtility.GetExternalSupportPower(board, receiver);
    }

    private static bool CanFight(UnitPiece unit)
    {
        return unit != null && !unit.IsDefeated && unit.Definition != null;
    }

    private static int GetPower(UnitPiece unit)
    {
        if (unit == null || unit.Definition == null)
            return 0;

        return unit.Definition.Power;
    }

    private static int GetSupport(UnitPiece unit)
    {
        if (unit == null || unit.Definition == null)
            return 0;

        return unit.Definition.Support;
    }

    private static bool CellsOverlap(
        IReadOnlyList<TriangleCell> aCells,
        IReadOnlyList<TriangleCell> bCells
    )
    {
        if (aCells == null || bCells == null)
            return false;

        HashSet<TriangleCell> bSet = new();

        foreach (TriangleCell cell in bCells)
        {
            if (cell != null)
                bSet.Add(cell);
        }

        foreach (TriangleCell cell in aCells)
        {
            if (cell != null && bSet.Contains(cell))
                return true;
        }

        return false;
    }

    private static bool CellsTouchOrOverlap(
        IReadOnlyList<TriangleCell> sourceCells,
        IReadOnlyList<TriangleCell> targetCells
    )
    {
        if (sourceCells == null || targetCells == null)
            return false;

        HashSet<TriangleCell> targetSet = new();
        HashSet<TriangleNode> targetCorners = new();

        foreach (TriangleCell targetCell in targetCells)
        {
            if (targetCell == null)
                continue;

            targetSet.Add(targetCell);

            if (targetCell.corners == null)
                continue;

            foreach (TriangleNode corner in targetCell.corners)
            {
                if (corner != null)
                    targetCorners.Add(corner);
            }
        }

        foreach (TriangleCell sourceCell in sourceCells)
        {
            if (sourceCell == null)
                continue;

            if (targetSet.Contains(sourceCell))
                return true;

            if (sourceCell.corners == null)
                continue;

            foreach (TriangleNode corner in sourceCell.corners)
            {
                if (corner != null && targetCorners.Contains(corner))
                    return true;
            }
        }

        return false;
    }
}