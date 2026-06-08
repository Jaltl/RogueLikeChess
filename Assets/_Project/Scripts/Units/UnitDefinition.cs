using UnityEngine;

[CreateAssetMenu(menuName = "Game/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    public string unitName;
    public Sprite icon;
    public UnitPiece unitPrefab;

    public int cost;
    public int placementExpansion = 1;

    [Header("Placement Footprint")]
    public float footprintRadius = 0.25f;
    public float footprintRotationDegrees = 45f;
}