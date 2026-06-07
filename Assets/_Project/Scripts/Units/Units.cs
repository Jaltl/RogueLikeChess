using UnityEngine;

public enum PlayerSide
{
    White,
    Black
}

public class Units : MonoBehaviour
{
    public UnitDefinition definition;
    public PlayerSide owner;
    public Vector2Int axialPosition;

    public void Init(UnitDefinition definition, PlayerSide owner, Vector2Int position)
    {
        this.definition = definition;
        this.owner = owner;
        axialPosition = position;
    }

    public void SetPosition(Vector2Int position)
    {
        axialPosition = position;
    }
}