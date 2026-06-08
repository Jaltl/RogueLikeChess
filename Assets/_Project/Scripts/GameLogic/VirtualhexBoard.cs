using System.Collections.Generic;
using UnityEngine;

public class VirtualHexBoard : MonoBehaviour
{
    private Dictionary<HexTile, UnitPiece> unitsByAnchor = new();

    public bool HasUnit(HexTile hex)
    {
        return hex != null && hex.IsOccupied;
    }

    public UnitPiece GetUnit(HexTile hex)
    {
       if(hex == null)
        {
            return null;
        }

        return hex.occupyingUnit;
    }

    public void PlaceUnit(UnitPiece unit, HexTile anchorHex, List<HexTile> occupiedHexes)
    {
        if(unit == null || anchorHex == null)
        {
            return;
        }

        unitsByAnchor[anchorHex] = unit;

        unit.SetAnchorHex(anchorHex);
        unit.SetOccupiedHexes(occupiedHexes);

        unit.transform.position = anchorHex.hexCenter;

        foreach (HexTile hex in occupiedHexes)
        {
            hex.SetOccupied(unit);
        }
    }

    public void RemoveUnit(UnitPiece unit)
    {
        if(unit == null)
        {
            return;
        }

        if(unit.anchorHex != null && unitsByAnchor.ContainsKey(unit.anchorHex))
        {
            unitsByAnchor.Remove(unit.anchorHex);
        }

        foreach (HexTile hex in unit.occupiedHexes)
        {
            hex.ClearOccupied(unit);
        }

        unit.ClearOccupiedHexes();
    }
}