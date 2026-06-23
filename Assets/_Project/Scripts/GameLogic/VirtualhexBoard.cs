using System.Collections.Generic;
using UnityEngine;

public class VirtualHexBoard : MonoBehaviour
{
    private Dictionary<GridPoint, UnitPiece> unitsByAnchor = new();

    public bool HasUnit(GridPoint point)
    {
        return point != null && point.IsOccupied;
    }

    public UnitPiece GetUnit(GridPoint point)
    {
       if(point == null)
        {
            return null;
        }

        return point.occupyingUnit;
    }

    public void PlaceUnit(UnitPiece unit, GridPoint anchorPoint, List<GridPoint> occupiedPoints)
    {
        if(unit == null || anchorPoint == null)
        {
            return;
        }

        unitsByAnchor[anchorPoint] = unit;

        unit.SetAnchorPoint(anchorPoint);
        unit.SetOccupiedPoints(occupiedPoints);

        unit.transform.position = anchorPoint.WorldPosition;

        foreach (GridPoint point in occupiedPoints)
        {
            point.SetOccupyingUnit(unit);
        }
    }

    public void RemoveUnit(UnitPiece unit)
    {
        if(unit == null)
        {
            return;
        }

        if(unit.anchorPoint != null && unitsByAnchor.ContainsKey(unit.anchorPoint))
        {
            unitsByAnchor.Remove(unit.anchorPoint);
        }

        foreach (GridPoint point in unit.occupiedPoints)
        {
            point.ClearOccupyingUnit(unit);
        }

        unit.ClearOccupiedPoints();
    }
}