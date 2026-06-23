using System.Collections.Generic;
using UnityEngine;

public enum PlayerSide
{
    White,
    Black
}

public class UnitPiece : MonoBehaviour
{
    public UnitDefinition definition;
    public PlayerSide owner;

    public GridPoint anchorPoint { get; private set; }
    public List<GridPoint> occupiedPoints { get; private set; } = new();

    public void Init(UnitDefinition definition, PlayerSide owner, GridPoint anchorPoint)
    {
        this.definition = definition;
        this.owner = owner;
        this.anchorPoint = anchorPoint;
    }

    public void SetAnchorPoint(GridPoint point)
    {
        anchorPoint = point;
    }

    public void SetOccupiedPoints(List<GridPoint> points)
    {
        occupiedPoints = points;
    }

    public void ClearOccupiedPoints()
    {
        occupiedPoints.Clear();
    }
}