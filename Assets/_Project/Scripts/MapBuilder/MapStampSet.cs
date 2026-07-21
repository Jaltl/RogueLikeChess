using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "Game/Map Stamp Set")]
public class MapStampSetDefinition : ScriptableObject
{
    [SerializeField] private string setName;
    [SerializeField] private MapStampStage stage;
    [SerializeField] private List<MapStampDefinition> stamps = new();

    public string SetName => string.IsNullOrWhiteSpace(setName) ? name : setName;
    public MapStampStage Stage => stage;
    public IReadOnlyList<MapStampDefinition> Stamps => stamps;
}
