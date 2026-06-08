using System.Collections.Generic;
using UnityEngine;

public class PlacementController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGridManager grid;
    [SerializeField] private VirtualHexBoard board;

    [Header("AI")]
    [SerializeField] private BlackPlacementAI blackAI;

    private UnitDefinition selectedUnit;
    private PlayerSide currentPlayer = PlayerSide.White;
    public bool HasSelectedUnit => selectedUnit != null;

    private HashSet<HexTile> whitePlacementZone = new();
    private HashSet<HexTile> blackPlacementZone = new();

    public PlayerSide CurrentPlayer => currentPlayer;

    public void InitializePlacement()
    {
        CreateInitialPlacementZones();

        Debug.Log($"White zone count: {whitePlacementZone.Count}");
        Debug.Log($"Black zone count: {blackPlacementZone.Count}");

        ShowCurrentPlacementZone();
    }

    void CreateInitialPlacementZones()
    {
        whitePlacementZone.Clear();
        blackPlacementZone.Clear();

        foreach (HexTile tile in grid.GetAllTiles())
        {
            Vector2Int pos = tile.axial;

            if (pos.y <= 2)
                whitePlacementZone.Add(tile);

            if (pos.y >= grid.Height - 3)
                blackPlacementZone.Add(tile);
        }
    }

    public void SelectUnit(UnitDefinition unit)
    {
        selectedUnit = unit;

        Debug.Log($"Selected unit: {unit.unitName}");

        HighlightPlacementTiles();
    }

    public void OnHexClicked(HexTile tile)
    {
        if (selectedUnit == null)
            return;

        if (!CanPlaceUnit(selectedUnit, tile))
            return;

        PlaceSelectedUnit(tile);
    }

    bool CanPlaceUnit(UnitDefinition unitDef, HexTile anchorHex)
    {
        return CanPlaceUnitForSide(unitDef, currentPlayer, anchorHex);
    }

    bool IsFootprintHexValid(HexTile hex, HashSet<HexTile> zone)
    {
        if (hex == null)
            return false;

        if (!zone.Contains(hex))
            return false;

        if (hex.IsOccupied)
            return false;

        if (hex.IsBlockedTerrain)
            return false;

        return true;
    }

    bool CanPlaceUnitForSide(UnitDefinition unitDef, PlayerSide owner, HexTile anchorHex)
    {
        if (unitDef == null || anchorHex == null)
            return false;

        List<HexTile> footprint =
            grid.GetHexesOverlappedByUnit(anchorHex, unitDef);

        if (footprint.Count == 0)
            return false;

        HashSet<HexTile> zone = GetPlacementZone(owner);

        foreach (HexTile hex in footprint)
        {
            if (!IsFootprintHexValid(hex, zone))
                return false;
        }

        return true;
    }

    void PlaceSelectedUnit(HexTile anchorHex)
    {
        if (selectedUnit == null)
            return;

        UnitDefinition unitToPlace = selectedUnit;

        bool placed = TryPlaceUnit(unitToPlace, currentPlayer, anchorHex);

        if (!placed)
            return;

        selectedUnit = null;
        ClearHighlights();
        ClearPreview();

        if (currentPlayer == PlayerSide.White && blackAI != null)
        {
            blackAI.PlaceAfterDelay();
        }
    }

    public bool TryPlaceUnit(UnitDefinition unitDef, PlayerSide owner, HexTile anchorHex)
    {
        if (unitDef == null || anchorHex == null)
            return false;

        if (!CanPlaceUnitForSide(unitDef, owner, anchorHex))
            return false;

        List<HexTile> overlappedHexes =
            grid.GetHexesOverlappedByUnit(anchorHex, unitDef);

        UnitPiece unit = Instantiate(unitDef.unitPrefab, anchorHex.hexCenter, Quaternion.Euler(0f, 0f, unitDef.footprintRotationDegrees));

        unit.Init(unitDef, owner, anchorHex);

        board.PlaceUnit(unit, anchorHex, overlappedHexes);

        ExpandPlacementZoneFromOccupiedHexes(
            overlappedHexes,
            unitDef.placementExpansion,
            owner
        );

        ShowCurrentPlacementZone();

        Debug.Log($"{owner} placed {unitDef.unitName} at {anchorHex.axial}");

        return true;
    }

    HashSet<HexTile> GetCurrentPlacementZone()
    {
        return GetPlacementZone(currentPlayer);
    }

    HashSet<HexTile> GetPlacementZone(PlayerSide side)
    {
        return side == PlayerSide.White
            ? whitePlacementZone
            : blackPlacementZone;
    }

    void HighlightPlacementTiles()
    {
        ClearHighlights();
        ClearPreview();

        if (selectedUnit == null)
            return;

        foreach (HexTile hex in GetCurrentPlacementZone())
        {
            if (CanPlaceUnit(selectedUnit, hex))
            {
                hex.SetPlacementHighlight(true);
            }
        }
    }

    public void PreviewFootprint(HexTile anchorHex)
    {
        ClearPreview();

        if (selectedUnit == null || anchorHex == null)
            return;

        List<HexTile> footprint =
            grid.GetHexesOverlappedByUnit(anchorHex, selectedUnit);

        HashSet<HexTile> zone = GetCurrentPlacementZone();

        foreach (HexTile hex in footprint)
        {
            if (hex == null)
                continue;

            if (IsFootprintHexValid(hex, zone))
            {
                hex.SetFootprintPreview(true);
            }
            else
            {
                hex.SetInvalidPreview(true);
            }
        }
    }

    public void ClearPreview()
    {
        foreach (HexTile tile in grid.GetAllTiles())
        {
            tile.SetFootprintPreview(false);
            tile.SetInvalidPreview(false);
        }
    }

    void ClearHighlights()
    {
        foreach (HexTile tile in grid.GetAllTiles())
        {
            tile.SetPlacementHighlight(false);
        }
    }

    public List<HexTile> GetValidPlacementTiles(UnitDefinition unitDef, PlayerSide side)
    {
        List<HexTile> validTiles = new();
        HashSet<HexTile> zone = GetPlacementZone(side);

        foreach (HexTile hex in zone)
        {
            if (CanPlaceUnitForSide(unitDef, side, hex))
            {
                validTiles.Add(hex);
            }
        }

        return validTiles;
    }

    void ExpandPlacementZoneFromOccupiedHexes(
        List<HexTile> occupiedHexes,
        int range,
        PlayerSide side)
    {
        HashSet<HexTile> zone = GetPlacementZone(side);

        foreach (HexTile hex in occupiedHexes)
        {
            foreach (HexTile nearby in grid.GetHexesInRange(hex.axial, range))
            {
                zone.Add(nearby);
            }
        }
    }

    void ShowCurrentPlacementZone()
    {
        ClearZoneHighlights();

        foreach (HexTile tile in GetCurrentPlacementZone())
        {
            tile.SetZoneHighlight(true);
        }
    }

    void ClearZoneHighlights()
    {
        foreach (HexTile tile in grid.GetAllTiles())
        {
            tile.SetZoneHighlight(false);
        }
    }

    void SwitchTurn()
    {
        currentPlayer = currentPlayer == PlayerSide.White
            ? PlayerSide.Black
            : PlayerSide.White;
    }
}