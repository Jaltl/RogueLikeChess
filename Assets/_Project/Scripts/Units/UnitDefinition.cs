using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    public string unitName;
    public Sprite icon;
    public Units unitPrefab;

    public int cost;
    public int placementExpansion = 1;
}