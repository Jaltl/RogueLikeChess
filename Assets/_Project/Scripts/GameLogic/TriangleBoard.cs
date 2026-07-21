using System.Collections.Generic;
using UnityEngine;

public class TriangleBoard : MonoBehaviour
{
    private readonly Dictionary<TriangleNode, UnitPiece> unitsByAnchor = new();
    private readonly Dictionary<TriangleCell, UnitPiece> unitsByOccupiedCell = new();

    private readonly List<UnitPiece> units = new();

    public IReadOnlyList<UnitPiece> Units => units;

    public bool HasUnit(TriangleCell cell)
    {
        return cell != null && unitsByOccupiedCell.ContainsKey(cell);
    }

    public bool HasUnitAtAnchor(TriangleNode node)
    {
        return node != null && unitsByAnchor.ContainsKey(node);
    }

    public UnitPiece GetUnit(TriangleCell cell)
    {
        if (cell == null)
            return null;

        unitsByOccupiedCell.TryGetValue(cell, out UnitPiece unit);
        return unit;
    }

    public bool IsCellOccupied(TriangleCell cell)
    {
        return cell != null && unitsByOccupiedCell.ContainsKey(cell);
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

            unitsByOccupiedCell[cell] = unit;
        }
    }

    public void RemoveUnit(UnitPiece unit)
    {
        if (unit == null)
            return;

        units.Remove(unit);

        if (unit.AnchorNode != null && unitsByAnchor.ContainsKey(unit.AnchorNode))
            unitsByAnchor.Remove(unit.AnchorNode);

        foreach (TriangleCell cell in unit.OccupiedCells)
        {
            if (cell == null)
                continue;

            if (unitsByOccupiedCell.TryGetValue(cell, out UnitPiece occupyingUnit) &&
                occupyingUnit == unit)
            {
                unitsByOccupiedCell.Remove(cell);
            }
        }

        unit.ClearRuntimeCells();
    }
}