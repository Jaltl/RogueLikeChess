using Unity.VisualScripting;
using UnityEngine;

public class Gridmanager : MonoBehaviour
{
    [SerializeField] private int _width, _height;
    [SerializeField] private Tile _tilePrefab;

    void GenerateGrid()
    {
        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var spawnedTile = Instantiate(Resources.Load("Tile"), new Vector3(x, y), Quaternion.identity) as GameObject;
                spawnedTile.name = $"Tile {x} {y}";
            }
        }
    }
}
