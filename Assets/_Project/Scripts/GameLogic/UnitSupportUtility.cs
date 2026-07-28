using UnityEngine;
using System.Collections.Generic;

public class UnitSupportLink
{
    public UnitPiece supporter;
    public UnitPiece receiver;
    public int supportAmount;
}

public static class UnitSupportUtility
{
    public static List<UnitSupportLink> GetSupportLinksForSide(
        TriangleBoard board,
        PlayerSide side
    )
    {
        List<UnitSupportLink> links = new();

        if (board == null)
            return links;

        foreach (UnitPiece receiver in board.Units)
        {
            if (!CanReceiveSupport(receiver, side))
                continue;

            foreach (UnitPiece supporter in board.Units)
            {
                if (!CanProvideSupport(supporter, side))
                    continue;

                if (supporter == receiver)
                    continue;

                if (CellsTouchOrOverlap(receiver.OccupiedCells, supporter.SupportCells))
                {
                    int amount = supporter.Definition != null
                        ? supporter.Definition.Support
                        : 0;

                    if (amount <= 0)
                        continue;

                    links.Add(new UnitSupportLink
                    {
                        supporter = supporter,
                        receiver = receiver,
                        supportAmount = amount
                    });
                }
            }
        }

        return links;
    }

    public static int GetExternalSupportPower(
        TriangleBoard board,
        UnitPiece receiver
    )
    {
        if (board == null || receiver == null || receiver.IsDefeated)
            return 0;

        int total = 0;

        foreach (UnitPiece supporter in board.Units)
        {
            if (supporter == null)
                continue;

            if (supporter == receiver)
                continue;

            if (supporter.IsDefeated)
                continue;

            if (supporter.Owner != receiver.Owner)
                continue;

            if (supporter.Definition == null)
                continue;

            if (supporter.Definition.Support <= 0)
                continue;

            if (CellsTouchOrOverlap(receiver.OccupiedCells, supporter.SupportCells))
                total += supporter.Definition.Support;
        }

        return total;
    }

    private static bool CanReceiveSupport(UnitPiece unit, PlayerSide side)
    {
        return unit != null &&
               !unit.IsDefeated &&
               unit.Owner == side &&
               unit.Definition != null;
    }

    private static bool CanProvideSupport(UnitPiece unit, PlayerSide side)
    {
        return unit != null &&
               !unit.IsDefeated &&
               unit.Owner == side &&
               unit.Definition != null &&
               unit.Definition.Support > 0;
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