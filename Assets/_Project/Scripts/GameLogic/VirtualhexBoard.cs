using System.Collections.Generic;
using UnityEngine;

public class VirtualHexBoard : MonoBehaviour
{
    private Dictionary<Vector2Int, Units> units = new();

    public bool HasUnit(Vector2Int pos)
    {
        return units.ContainsKey(pos);
    }

    public Units GetUnit(Vector2Int pos)
    {
        units.TryGetValue(pos, out Units unit);
        return unit;
    }

    public void PlaceUnit(Units unit, Vector2Int pos)
    {
        units[pos] = unit;
        unit.SetPosition(pos);
    }

    public void RemoveUnit(Vector2Int pos)
    {
        units.Remove(pos);
    }
}