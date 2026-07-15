using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitPlacementResult
{
    public TriangleNode anchorNode;
    public TriangleCell anchorCell;

    public List<TriangleCell> baseCells = new();
    public List<TriangleCell> supportCells = new();

    public Vector3 AnchorWorldPosition
    {
        get
        {
            if (anchorNode == null)
                return Vector3.zero;

            return anchorNode.worldPosition;
        }
    }
}

public static class UnitFootprintResolver
{
    public static bool TryResolvePlacementFromWorld(
        TriangleGridManager grid,
        UnitDefinition unit,
        Vector3 worldPosition,
        float maxSnapDistance,
        out UnitPlacementResult result,
        Func<TriangleCell, bool> extraBaseCellValidity = null
    )
    {
        result = null;

        if (grid == null || unit == null)
            return false;

        TriangleNode anchorNode = grid.FindClosestAnchorNode(worldPosition, unit.anchorType, maxSnapDistance);

        if (anchorNode == null)
            return false;

        return TryResolvePlacementFromAnchorNode(
            grid,
            unit,
            anchorNode,
            out result,
            extraBaseCellValidity
        );
    }

    public static bool TryResolvePlacementFromAnchorNode(
        TriangleGridManager grid,
        UnitDefinition unit,
        TriangleNode anchorNode,
        out UnitPlacementResult result,
        Func<TriangleCell, bool> extraBaseCellValidity = null
    )
    {
        result = null;

        if (grid == null || unit == null || anchorNode == null)
            return false;

        if (!anchorNode.SupportsUnitAnchorType(unit.anchorType))
            return false;

        foreach (TriangleCell possibleAnchorCell in anchorNode.ownerTriangles)
        {
            if (possibleAnchorCell == null)
                continue;

            if (TryResolvePlacementFromAnchorCell(
                    grid,
                    unit,
                    anchorNode,
                    possibleAnchorCell,
                    out UnitPlacementResult candidate,
                    extraBaseCellValidity
                ))
            {
                result = candidate;
                return true;
            }
        }

        return false;
    }

    public static bool TryResolvePlacementFromAnchorCell(
        TriangleGridManager grid,
        UnitDefinition unit,
        TriangleNode anchorNode,
        TriangleCell anchorCell,
        out UnitPlacementResult result,
        Func<TriangleCell, bool> extraBaseCellValidity = null
    )
    {
        result = null;

        if (grid == null || unit == null || anchorCell == null)
            return false;

        if (unit.baseSize == null || unit.baseSize.Count == 0)
            return false;

        IReadOnlyList<TriangleFootprintCell> baseFootprint = unit.GetBaseFootprint(anchorCell.orientation);

        List<TriangleCell> baseCells = ResolveCells(
            grid,
            anchorCell,
            baseFootprint,
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

        IReadOnlyList<TriangleFootprintCell> supportFootprint = unit.GetSupportFootprint(anchorCell.orientation);

        List<TriangleCell> supportCells = ResolveCells(
            grid,
            anchorCell,
            supportFootprint,
            onlyActive: true
        );

        HashSet<TriangleCell> baseSet = new(baseCells);
        supportCells.RemoveAll(cell => cell == null || baseSet.Contains(cell));

        result = new UnitPlacementResult
        {
            anchorNode = anchorNode,
            anchorCell = anchorCell,
            baseCells = baseCells,
            supportCells = supportCells
        };

        return true;
    }

    public static List<TriangleCell> ResolveCells(
    TriangleGridManager grid,
    TriangleCell anchorCell,
    IReadOnlyList<TriangleFootprintCell> footprint,
    bool onlyActive = false
)
    {
        List<TriangleCell> result = new();

        if (grid == null || anchorCell == null || footprint == null)
            return result;

        TriangleMapDefinition map = grid.MapDefinition;

        int anchorProfileX = map != null
            ? map.GetProfileColumn(anchorCell.coord)
            : anchorCell.coord.x;

        foreach (TriangleFootprintCell footprintCell in footprint)
        {
            int targetRow = anchorCell.coord.y + footprintCell.y;
            int targetProfileX = anchorProfileX + footprintCell.x;

            Vector2Int coord;

            if (map != null)
                coord = map.GetCoordFromProfileColumn(targetProfileX, targetRow);
            else
                coord = new Vector2Int(targetProfileX, targetRow);

            TriangleCell cell = grid.GetCell(coord);

            if (cell == null)
                continue;

            if (onlyActive && !cell.isActive)
                continue;

            if (!result.Contains(cell))
                result.Add(cell);
        }

        return result;
    }
}