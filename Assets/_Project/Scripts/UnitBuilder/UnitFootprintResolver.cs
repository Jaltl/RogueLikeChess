using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitPlacementResult
{
    public TriangleNode anchorNode;
    public TriangleCell anchorCell;
    public List<TriangleCell> baseCells = new();
    public List<TriangleCell> supportCells = new();
    public UnitFootprintFacing facing;

    public Vector3 AnchorWorldPosition => anchorNode == null ? Vector3.zero : anchorNode.worldPosition;
}

public static class UnitFootprintResolver
{
    public static bool TryResolvePlacementFromWorld(
        TriangleGridManager grid,
        UnitDefinition unit,
        Vector3 worldPosition,
        UnitFootprintFacing facing,
        float maxSnapDistance,
        out UnitPlacementResult result,
        Func<TriangleCell, bool> extraBaseCellValidity = null
    )
    {
        result = null;

        if (grid == null || unit == null)
            return false;

        TriangleNode anchorNode = grid.FindClosestAnchorNode(
            worldPosition,
            UnitAnchorType.Corner,
            maxSnapDistance
        );

        if (anchorNode == null)
            return false;

        return TryResolvePlacementFromAnchorNode(
            grid,
            unit,
            anchorNode,
            facing,
            out result,
            extraBaseCellValidity
        );
    }

    public static bool TryResolvePlacementFromAnchorNode(
        TriangleGridManager grid,
        UnitDefinition unit,
        TriangleNode anchorNode,
        UnitFootprintFacing facing,
        out UnitPlacementResult result,
        Func<TriangleCell, bool> extraBaseCellValidity = null
    )
    {
        result = null;

        if (grid == null || unit == null || anchorNode == null)
            return false;

        if (!anchorNode.SupportsUnitAnchorType(UnitAnchorType.Corner))
            return false;

        IReadOnlyList<TriangleFootprintCell> baseFootprint = unit.GetFootprint(UnitFootprintArea.BaseSize);
        IReadOnlyList<TriangleFootprintCell> supportFootprint = unit.GetFootprint(UnitFootprintArea.SupportRange);

        if (baseFootprint == null || baseFootprint.Count == 0)
            return false;

        List<TriangleCell> baseCells = ResolveCells(
            grid,
            anchorNode,
            baseFootprint,
            facing,
            onlyActive: false
        );

        if (baseCells.Count != baseFootprint.Count)
            return false;

        foreach (TriangleCell cell in baseCells)
        {
            if (cell == null)
                return false;

            if (!cell.isActive)
                return false;

            if (cell.isBlocked)
                return false;

            if (extraBaseCellValidity != null && !extraBaseCellValidity(cell))
                return false;
        }

        List<TriangleCell> supportCells = ResolveCells(
            grid,
            anchorNode,
            supportFootprint,
            facing,
            onlyActive: true
        );

        HashSet<TriangleCell> baseSet = new(baseCells);
        supportCells.RemoveAll(cell => cell == null || baseSet.Contains(cell));

        result = new UnitPlacementResult
        {
            anchorNode = anchorNode,
            anchorCell = GetFirstOwnerCell(anchorNode),
            baseCells = baseCells,
            supportCells = supportCells,
            facing = facing
        };

        return true;
    }

    private static TriangleCell GetFirstOwnerCell(TriangleNode anchorNode)
    {
        if (anchorNode == null)
            return null;

        foreach (TriangleCell cell in anchorNode.ownerTriangles)
            return cell;

        return null;
    }

    private static List<TriangleCell> ResolveCells(
        TriangleGridManager grid,
        TriangleNode anchorNode,
        IReadOnlyList<TriangleFootprintCell> footprint,
        UnitFootprintFacing facing,
        bool onlyActive
    )
    {
        List<TriangleCell> result = new();

        if (grid == null || anchorNode == null || footprint == null)
            return result;

        float cellSnapDistance = grid.MapDefinition != null
            ? grid.MapDefinition.sideLength * 0.6f
            : 0.6f;

        foreach (TriangleFootprintCell footprintCell in footprint)
        {
            float sideLength = grid.MapDefinition != null ? grid.MapDefinition.sideLength : 1f;

            Vector2 localWorldOffset = new Vector2(
                footprintCell.localX * sideLength,
                footprintCell.localY * sideLength
            );

            Vector2 rotatedOffset = RotateOffset(
                localWorldOffset,
                facing
            );

            Vector3 targetCenter = anchorNode.worldPosition + new Vector3(
                rotatedOffset.x,
                rotatedOffset.y,
                0f
            );

            TriangleCell cell = grid.FindClosestCellCenter(targetCenter, cellSnapDistance);

            if (cell == null)
                continue;

            if (onlyActive && !cell.isActive)
                continue;

            if (!result.Contains(cell))
                result.Add(cell);
        }

        return result;
    }

    private static Vector2 RotateOffset(Vector2 offset, UnitFootprintFacing facing)
    {
        float angle = -(int)facing * 60f;
        float radians = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);

        return new Vector2(
            offset.x * cos - offset.y * sin,
            offset.x * sin + offset.y * cos
        );
    }
}