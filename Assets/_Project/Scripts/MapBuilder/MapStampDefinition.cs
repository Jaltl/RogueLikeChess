using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Game/Map Stamp Definition")]
public class MapStampDefinition : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string stampName;
    [SerializeField] private MapStampCategory category;
    [SerializeField] private MapStampStage stage;

    [Header("Generation")]
    [SerializeField] private int weight;
    [SerializeField] private bool allowRotation = true;
    [SerializeField] private bool mirrorForOpposite = false;

    [Header("Footprint")]
    [SerializeField] private List<MapStampCell> cells = new();

    [Header("Visuals")]
    [SerializeField] private List<MapStampVisual> visuals = new();

    [Header("Rules")]
    [SerializeField] private List<MapStampPlacementRules> rules = new();

    public string StampName => string.IsNullOrWhiteSpace(stampName) ? name : stampName;
    public MapStampCategory Category => category;
    public MapStampStage Stage => stage;
    public int Weight => Mathf.Max(1,weight);
    public bool AllowRotation => allowRotation;
    public bool MirrorForOpposite => mirrorForOpposite;
    public IReadOnlyList<MapStampCell> Cells => cells;
    public IReadOnlyList<MapStampVisual> Visuals => visuals;
    public IReadOnlyList<MapStampPlacementRules> Rules => rules;
}