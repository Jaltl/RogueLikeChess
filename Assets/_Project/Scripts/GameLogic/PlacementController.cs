using System.Collections.Generic;
using UnityEngine;

public class PlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PointGridManager grid;
    [SerializeField] private VisualTriangleRenderer visualTriangles;

    [Header("AI")]
    [SerializeField] private BlackPlacementAI blackAI;

    [Header("Starting Zones")]
    [SerializeField] private int startingZoneRows = 5;

    private UnitDefinition selectedUnit;
    private PlayerSide currentPlayer = PlayerSide.White;

    private HashSet<GridPoint> whitePlacementZone = new();
    private HashSet<GridPoint> blackPlacementZone = new();

    public bool HasSelectedUnit => selectedUnit != null;
    public PlayerSide CurrentPlayer => currentPlayer;

    public void InitializePlacement()
    {
        CreateInitialPlacementZones();

        Debug.Log($"White zone count: {whitePlacementZone.Count}");
        Debug.Log($"Black zone count: {blackPlacementZone.Count}");

        //ShowCurrentPlacementZone();
        HighlightPlacementPoints();
    }

    void CreateInitialPlacementZones()
    {
        whitePlacementZone.Clear();
        blackPlacementZone.Clear();

        foreach (GridPoint point in grid.GetAllPoints())
        {
            Vector2Int pos = point.coordinates;

            if (pos.y < startingZoneRows)
                whitePlacementZone.Add(point);

            if (pos.y >= grid.Height - startingZoneRows)
                blackPlacementZone.Add(point);
        }
    }

    public void SelectUnit(UnitDefinition unit)
    {
        selectedUnit = unit;

        Debug.Log($"Selected unit: {unit.unitName}");

        ShowCurrentPlacementZone();
        HighlightPlacementPoints();
    }

    public void OnGridPointClicked(GridPoint point)
    {
        if (selectedUnit == null)
            return;

        if (!CanPlaceUnit(selectedUnit, point))
            return;

        PlaceSelectedUnit(point);
    }

    bool CanPlaceUnit(UnitDefinition unitDef, GridPoint anchorPoint)
    {
        return CanPlaceUnitForSide(unitDef, currentPlayer, anchorPoint);
    }

    bool CanPlaceUnitForSide(UnitDefinition unitDef, PlayerSide owner, GridPoint anchorPoint)
    {
        if (unitDef == null || anchorPoint == null)
            return false;

        List<GridPoint> footprint = grid.GetPointsInsideUnitFootprint(anchorPoint, unitDef);

        if (footprint.Count == 0)
            return false;

        HashSet<GridPoint> zone = GetPlacementZone(owner);

        foreach (GridPoint point in footprint)
        {
            if (point == null)
                return false;

            if (!point.IsActive)
                return false;

            if (!zone.Contains(point))
                return false;

            if (point.IsOccupied)
                return false;

            if (point.IsBlockedTerrain)
                return false;
        }

        return true;
    }

    void PlaceSelectedUnit(GridPoint anchorPoint)
    {
        if (selectedUnit == null)
            return;

        UnitDefinition unitToPlace = selectedUnit;

        bool placed = TryPlaceUnit(unitToPlace, currentPlayer, anchorPoint);

        if (!placed)
            return;

        selectedUnit = null;

        ClearPlacementHighlights();
        ClearPreview();
        ShowCurrentPlacementZone();

        if (currentPlayer == PlayerSide.White && blackAI != null)
        {
            blackAI.PlaceAfterDelay();
        }
    }

    public bool TryPlaceUnit(UnitDefinition unitDef, PlayerSide owner, GridPoint anchorPoint)
    {
        if (unitDef == null || anchorPoint == null)
            return false;

        if (!CanPlaceUnitForSide(unitDef, owner, anchorPoint))
            return false;

        List<GridPoint> occupiedPoints =
            grid.GetPointsInsideUnitFootprint(anchorPoint, unitDef);

        UnitPiece unit = Instantiate(
            unitDef.unitPrefab,
            anchorPoint.WorldPosition,
            Quaternion.Euler(0f, 0f, unitDef.footprintRotationDegrees)
        );

        unit.Init(unitDef, owner, anchorPoint);
        unit.SetOccupiedPoints(occupiedPoints);

        foreach (GridPoint point in occupiedPoints)
        {
            point.SetOccupyingUnit(unit);
        }

        ExpandPlacementZoneFromOccupiedPoints(
            occupiedPoints,
            unitDef.placementExpansion,
            owner
        );

        Debug.Log($"{owner} placed {unitDef.unitName} at {anchorPoint.coordinates}");

        return true;
    }

    public List<GridPoint> GetValidPlacementPoints(UnitDefinition unitDef, PlayerSide side)
    {
        List<GridPoint> validPoints = new();
        HashSet<GridPoint> zone = GetPlacementZone(side);

        foreach (GridPoint point in zone)
        {
            if (CanPlaceUnitForSide(unitDef, side, point))
            {
                validPoints.Add(point);
            }
        }

        return validPoints;
    }

    public void PreviewFootprint(GridPoint anchorPoint)
    {
        ClearPreview();

        if (selectedUnit == null || anchorPoint == null)
        {
            RefreshVisuals();
            return;
        }

        List<GridPoint> footprint =
            grid.GetPointsInsideUnitFootprint(anchorPoint, selectedUnit);

        bool canPlace = CanPlaceUnit(selectedUnit, anchorPoint);

        foreach (GridPoint point in footprint)
        {
            if (canPlace)
                point.SetFootprintPreview(true);
            else
                point.SetInvalidPreview(true);
    }

    RefreshVisuals();
}

    public void ClearPreview()
    {
        foreach (GridPoint point in grid.GetAllPoints())
        {
            point.ClearPreview();
        }

        RefreshVisuals();
    }

    void HighlightPlacementPoints()
    {
        ClearPlacementHighlights();
        ClearPreview();

        if (selectedUnit == null)
            return;

        foreach (GridPoint point in GetCurrentPlacementZone())
        {
            if (CanPlaceUnit(selectedUnit, point))
            {
                point.SetPlacementHighlight(true);
            }
        }
    }

    void ClearPlacementHighlights()
    {
        foreach (GridPoint point in grid.GetAllPoints())
        {
            point.SetPlacementHighlight(false);
        }
    }

    void ShowCurrentPlacementZone()
    {
        ClearZoneHighlights();

        foreach (GridPoint point in GetCurrentPlacementZone())
        {
            point.SetZoneHighlight(true);
        }
    }

    void ClearZoneHighlights()
    {
        foreach (GridPoint point in grid.GetAllPoints())
        {
            point.SetZoneHighlight(false);
        }
    }

    public void OnGridPointHoverEnter(GridPoint point)
{
    if (point == null)
        return;

    if (HasSelectedUnit)
    {
        PreviewFootprint(point);
    }
    else
    {
        point.SetHoverHighlight(true);
        RefreshVisuals();
    }
}

public void OnGridPointHoverExit(GridPoint point)
{
    if (point == null)
        return;

    point.SetHoverHighlight(false);
    ClearPreview();
    RefreshVisuals();
}

void RefreshVisuals()
{
    if (visualTriangles != null)
        visualTriangles.RefreshLineColors();
}

    HashSet<GridPoint> GetCurrentPlacementZone()
    {
        return GetPlacementZone(currentPlayer);
    }

    HashSet<GridPoint> GetPlacementZone(PlayerSide side)
    {
        return side == PlayerSide.White
            ? whitePlacementZone
            : blackPlacementZone;
    }

    void ExpandPlacementZoneFromOccupiedPoints(
        List<GridPoint> occupiedPoints,
        int range,
        PlayerSide side)
    {
        HashSet<GridPoint> zone = GetPlacementZone(side);

        foreach (GridPoint point in occupiedPoints)
        {
            foreach (GridPoint nearby in grid.GetNeighborsInRange(point.coordinates, range))
            {
                zone.Add(nearby);
            }
        }
    }

    void SwitchTurn()
    {
        currentPlayer = currentPlayer == PlayerSide.White
            ? PlayerSide.Black
            : PlayerSide.White;

        selectedUnit = null;

        ClearPlacementHighlights();
        ClearPreview();
        ShowCurrentPlacementZone();
    }
}