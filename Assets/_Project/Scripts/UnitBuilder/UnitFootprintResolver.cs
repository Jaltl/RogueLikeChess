using System;
using System.Collections.Generic;
using UnityEngine;

public class UnitPlacementResult
{
    public TriangleNode anchorNode;
    public TriangleCell anchorCell;
    public List<TriangleCell> baseCells = new();
    public List<TriangleCell> supportCells = new();
}

public static class UnitFootprintResolver
{
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

            if (!TryResolvePlacementFromAnchorCell(
                    grid,
                    unit,
                    anchorNode,
                    possibleAnchorCell,
                    out UnitPlacementResult candidate,
                    extraBaseCellValidity
                ))
            {
                continue;
            }

            result = candidate;
            return true;
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

        List<TriangleCell> baseCells = ResolveCells(
            grid,
            anchorCell,
            unit.baseSize
        );

        if (baseCells.Count != unit.baseSize.Count)
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
            anchorCell,
            unit.supportRange
        );

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
        IReadOnlyList<TriangleFootprintCell> footprint
    )
    {
        List<TriangleCell> result = new();

        if (grid == null || anchorCell == null || footprint == null)
            return result;

        foreach (TriangleFootprintCell footprintCell in footprint)
        {
            Vector2Int coord = anchorCell.coord + footprintCell.Coord;
            TriangleCell cell = grid.GetCell(coord);

            if (cell != null)
                result.Add(cell);
        }

        return result;
    }
}