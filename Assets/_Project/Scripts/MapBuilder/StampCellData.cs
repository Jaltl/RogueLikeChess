using UnityEngine;
using System.Collections.Generic;

public enum MapStampCategory
{
    TerrainShape,
    StartZone,
    Objective,
    Forest,
    Rock,
    Water,
    Ruin,
    Decoration
}

public enum MapStampStage
{
    StructuralTerrain,
    StartZones,
    Objectives,
    Water,
    LargeBlockers,
    Forest,
    Decoration
}
public enum MapTerrainType
{
    Normal,
    Forest,
    HeavyForest,
    Rock,
    Cliff,
    ShallowWater,
    DeepWater,
    Road,
    Ruin,
    Objective,
    Deployment
}

[System.Flags]
public enum MapCellFlags
{
    None = 0,
    BlocksMovement = 1 << 0,
    BlocksPlacement = 1 << 1,
    BlocksLineOfSight = 1 << 2,
    ProvidesCover = 1 << 3,
    DifficultTerrain = 1 << 4,
    Water = 1 << 5,
    Reserved = 1 << 6
}


[System.Serializable]
public struct MapStampCell
{
    public float localX;
    public float localY;

    public bool setActive;
    public bool activeValue;

    public bool setTerrainType;
    public MapTerrainType terrainType;
    
    public MapCellFlags addFlags;
    public MapCellFlags removeFlags;

    public bool SetRegion;
    public MapRegion region;

    public Vector2 LocalOffset => new Vector2(localX, localY);
}

[System.Serializable]
public struct MapStampVisual
{
    public Sprite sprite;

    public float localX;
    public float localY;

    public Vector2 size;
    public float roationDegrees;
    public int sortingOrder;

    public Vector2 LocalOffset => new Vector2(localX, localY);
}

[System.Serializable]
public class MapStampPlacementRules
{
    [Header("Counts")]
    [SerializeField] private int minPlacements = 0;
    [SerializeField] private int maxPlacements = 3;

    [Header("Allowed Regions")]
    [SerializeField] private bool allowNeutral = true;
    [SerializeField] private bool allowWhiteStart = false;
    [SerializeField] private bool allowBlackStart = false;
    [SerializeField] private bool allowObjectives = false;

    [Header("Spacing")]
    [SerializeField] private int minDistanceFromSameCategory = 0;
    [SerializeField] private int minDistanceFromStartZones = 3;
    [SerializeField] private int minDistanceFromObjectives = 1;

    [Header("Overlap")]
    [SerializeField] private bool canOverlapSameCategory = false;
    [SerializeField] private bool canOverwriteExistingTerrain = false;
    [SerializeField] private MapStampCategory[] canOverwriteCategories;

    [Header("Connectivity")]
    [SerializeField] private bool requireMapStillConnected = true;

    public int MinPlacements => minPlacements;
    public int MaxPlacements => maxPlacements;
    public bool AllowNeutral => allowNeutral;
    public bool AllowWhiteStart => allowWhiteStart;
    public bool AllowBlackStart => allowBlackStart;
    public bool AllowObjectives => allowObjectives;
    public int MinDistanceFromSameCategory => minDistanceFromSameCategory;
    public int MinDistanceFromStartZones => minDistanceFromStartZones;
    public int MinDistanceFromObjectives => minDistanceFromObjectives;
    public bool CanOverlapSameCategory => canOverlapSameCategory;
    public bool CanOverwriteExistingTerrain => canOverwriteExistingTerrain;
    public IReadOnlyList<MapStampCategory> CanOverwriteCategories => canOverwriteCategories;
    public bool RequireMapStillConnected => requireMapStillConnected;

}

[System.Serializable]
public class MapGenerationStageRule
{
    [SerializeField] private MapStampStage stage;
    [SerializeField] private MapStampDefinition stampSet;

    [SerializeField] private int minPlacements = 0;
    [SerializeField] private int maxPlacements = 5;

    [SerializeField] private int attemptsPerPlacement = 40;

    public MapStampStage Stage => stage;
    public MapStampDefinition StampSet => stampSet;
    public int MinPlacements => minPlacements;
    public int MaxPlacements => maxPlacements;
    public int AttemptsPerPlacement => attemptsPerPlacement;
}