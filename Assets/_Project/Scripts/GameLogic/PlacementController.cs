using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlacementController : MonoBehaviour
{
    [SerializeField] private HexGridManager grid;
    [SerializeField] private VirtualHexBoard board;

    private UnitDefinition selectedUnit;
    private PlayerSide currentPlayer = PlayerSide.White;

    private HashSet<Vector2Int> whitePlacementZone = new();
    private HashSet<Vector2Int> blackPlacementZone = new();

    public void InitializePlacement()
    {
        CreateInitialPlacementZones();

        Debug.Log($"White zone count: {whitePlacementZone.Count}");
        Debug.Log($"Black zone count: {blackPlacementZone.Count}");
    }

    void CreateInitialPlacementZones()
    {
        whitePlacementZone.Clear();
        blackPlacementZone.Clear();

        foreach (var tile in grid.GetAllTiles())
        {
            Vector2Int pos = tile.axial;

            // Example:
            // White starts near bottom
            if (pos.y <= 1)
                whitePlacementZone.Add(pos);

            // Black starts near top
            if (pos.y >= 6)
                blackPlacementZone.Add(pos);
        }
    }

    public void SelectUnit(UnitDefinition unit)
    {
        selectedUnit = unit;
        HighlightPlacementTiles();
    }

    public void OnHexClicked(HexTile tile)
    {
        if (selectedUnit == null)
            return;

        Vector2Int pos = tile.axial;

        if (!CanPlaceAt(pos))
            return;

        PlaceSelectedUnit(pos);
    }

    bool CanPlaceAt(Vector2Int pos)
    {
        if (board.HasUnit(pos))
            return false;

        return GetCurrentPlacementZone().Contains(pos);
    }

    void PlaceSelectedUnit(Vector2Int pos)
    {
        Units unit = Instantiate(
            selectedUnit.unitPrefab,
            grid.AxialToWorld(pos),
            Quaternion.identity
        );

        unit.Init(selectedUnit, currentPlayer, pos);
        board.PlaceUnit(unit, pos);

        ExpandPlacementZone(pos, selectedUnit.placementExpansion);

        selectedUnit = null;

        ClearHighlights();

        // Optional:
        // SwitchTurn();
    }

    HashSet<Vector2Int> GetCurrentPlacementZone()
    {
        return currentPlayer == PlayerSide.White
            ? whitePlacementZone
            : blackPlacementZone;
    }

    void HighlightPlacementTiles()
    {
        ClearHighlights();

        foreach (Vector2Int pos in GetCurrentPlacementZone())
        {
            if (board.HasUnit(pos))
                continue;

            HexTile tile = grid.GetTile(pos);
            if (tile != null)
                tile.SetPlacementHighlight(true);
        }
    }

    void ClearHighlights()
    {
        foreach (var tile in grid.GetAllTiles())
        {
            tile.SetPlacementHighlight(false);
        }
    }

    void SwitchTurn()
    {
        currentPlayer = currentPlayer == PlayerSide.White
            ? PlayerSide.Black
            : PlayerSide.White;
    }

    void ExpandPlacementZone(Vector2Int center, int range)
    {
        HashSet<Vector2Int> zone = GetCurrentPlacementZone();

        foreach (Vector2Int pos in GetHexesInRange(center, range))
        {
            if (grid.IsInside(pos))
                zone.Add(pos);
        }
    }

    List<Vector2Int> GetHexesInRange(Vector2Int center, int range)
    {
        List<Vector2Int> results = new();

        for (int dq = -range; dq <= range; dq++)
        {
            for (int dr = -range; dr <= range; dr++)
            {
                Vector2Int pos = new Vector2Int(center.x + dq, center.y + dr);

                if (HexDistance(center, pos) <= range)
                    results.Add(pos);
            }
        }

        return results;
    }

    int HexDistance(Vector2Int a, Vector2Int b)
    {
        // axial q,r converted to cube coordinates
        int aq = a.x;
        int ar = a.y;
        int as_ = -aq - ar;

        int bq = b.x;
        int br = b.y;
        int bs = -bq - br;

        return Mathf.Max(
            Mathf.Abs(aq - bq),
            Mathf.Abs(ar - br),
            Mathf.Abs(as_ - bs)
        );
    }
}