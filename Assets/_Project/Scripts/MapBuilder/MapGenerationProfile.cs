using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Game/Map Generation Profile")]
public class MapGenerationProfile : ScriptableObject
{
    [SerializeField] private TriangleMapDefinition baseMap;
    [SerializeField] private List<MapGenerationStageRule> stages = new();

    public TriangleMapDefinition BaseMap => baseMap;
    public IReadOnlyList<MapGenerationStageRule> Stages => stages;
}
