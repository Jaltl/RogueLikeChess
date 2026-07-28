using System.Collections.Generic;
using UnityEngine;

public class TriangleBoard : MonoBehaviour
{
    private readonly List<UnitPiece> units = new();
    private readonly Dictionary<TriangleNode, UnitPiece> unitsByAnchor = new();
    private readonly Dictionary<TriangleCell, List<UnitPiece>> unitsByOccupiedCell = new();

    public IReadOnlyList<UnitPiece> Units => units;

    public bool HasUnitAtAnchor(TriangleNode node)
    {
        return node != null && unitsByAnchor.ContainsKey(node);
    }

    public bool IsCellOccupied(TriangleCell cell)
    {
        return cell != null &&
               unitsByOccupiedCell.TryGetValue(cell, out List<UnitPiece> cellUnits) &&
               cellUnits.Count > 0;
    }

    public UnitPiece GetUnit(TriangleCell cell)
    {
        if (cell == null)
            return null;

        if (!unitsByOccupiedCell.TryGetValue(cell, out List<UnitPiece> cellUnits))
            return null;

        for (int i = 0; i < cellUnits.Count; i++)
        {
            if (cellUnits[i] != null)
                return cellUnits[i];
        }

        return null;
    }

    public List<UnitPiece> GetUnits(TriangleCell cell)
    {
        if (cell == null)
            return new List<UnitPiece>();

        if (!unitsByOccupiedCell.TryGetValue(cell, out List<UnitPiece> cellUnits))
            return new List<UnitPiece>();

        return new List<UnitPiece>(cellUnits);
    }

    public HashSet<UnitPiece> GetUnitsOverlappingCells(IReadOnlyList<TriangleCell> cells)
    {
        HashSet<UnitPiece> result = new();

        if (cells == null)
            return result;

        foreach (TriangleCell cell in cells)
        {
            if (cell == null)
                continue;

            if (!unitsByOccupiedCell.TryGetValue(cell, out List<UnitPiece> cellUnits))
                continue;

            foreach (UnitPiece unit in cellUnits)
            {
                if (unit != null)
                    result.Add(unit);
            }
        }

        return result;
    }

    public bool CellBlocksPlacementForSide(TriangleCell cell, PlayerSide placingSide)
    {
        if (cell == null)
            return true;

        if (!unitsByOccupiedCell.TryGetValue(cell, out List<UnitPiece> cellUnits))
            return false;

        foreach (UnitPiece unit in cellUnits)
        {
            if (unit == null)
                continue;

            // Dead units are terrain blockers.
            if (unit.IsDefeated)
                return true;

            // Friendly units block placement.
            if (unit.Owner == placingSide)
                return true;

            // Living enemy units do not block placement.
            // They trigger direct-overlap conflict instead.
        }

        return false;
    }

    public void PlaceUnit(UnitPiece unit, UnitPlacementResult placement)
    {
        if (unit == null || placement == null || placement.anchorNode == null)
            return;

        if (!units.Contains(unit))
            units.Add(unit);

        unitsByAnchor[placement.anchorNode] = unit;

        foreach (TriangleCell cell in placement.baseCells)
        {
            if (cell == null)
                continue;

            if (!unitsByOccupiedCell.TryGetValue(cell, out List<UnitPiece> cellUnits))
            {
                cellUnits = new List<UnitPiece>();
                unitsByOccupiedCell[cell] = cellUnits;
            }

            if (!cellUnits.Contains(unit))
                cellUnits.Add(unit);
        }
    }

    public void RemoveUnit(UnitPiece unit)
    {
        if (unit == null)
            return;

        units.Remove(unit);

        if (unit.AnchorNode != null &&
            unitsByAnchor.TryGetValue(unit.AnchorNode, out UnitPiece anchoredUnit) &&
            anchoredUnit == unit)
        {
            unitsByAnchor.Remove(unit.AnchorNode);
        }

        foreach (TriangleCell cell in unit.OccupiedCells)
        {
            if (cell == null)
                continue;

            if (!unitsByOccupiedCell.TryGetValue(cell, out List<UnitPiece> cellUnits))
                continue;

            cellUnits.Remove(unit);

            if (cellUnits.Count == 0)
                unitsByOccupiedCell.Remove(cell);
        }

        unit.ClearRuntimeCells();
    }
}