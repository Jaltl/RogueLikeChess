using System.Collections.Generic;
using UnityEngine;

public class HexGridManager : MonoBehaviour
{
    [SerializeField] private int width = 8;
    [SerializeField] private int height = 8;
    [SerializeField] private HexTile hexPrefab;
    [SerializeField] private PlacementController placementController;

    private Dictionary<Vector2Int, HexTile> tiles = new();

    private const float hexWidth = 1f;
    private const float hexHeight = 0.8660254f; // sqrt(3)/2

    private void Start()
    {
        GenerateGrid();
    }

    void GenerateGrid()
    {
        for (int q = 0; q < width; q++)
        {
            for (int r = 0; r < height; r++)
            {
                Vector2Int axial = new Vector2Int(q, r);
                Vector3 worldPos = AxialToWorld(axial);

                HexTile tile = Instantiate(hexPrefab, worldPos, Quaternion.identity, transform);
                tile.name = $"Hex {q},{r}";
                tile.Init(axial, placementController);

                tiles[axial] = tile;
            }
        }

        placementController.InitializePlacement();
    }

    public HexTile GetTile(Vector2Int axial)
    {
        tiles.TryGetValue(axial, out HexTile tile);
        return tile;
    }

    public IEnumerable<HexTile> GetAllTiles()
    {
        return tiles.Values;
    }

    public bool IsInside(Vector2Int axial)
    {
        return tiles.ContainsKey(axial);
    }

    public Vector3 AxialToWorld(Vector2Int axial)
    {
        int q = axial.x;
        int r = axial.y;

        float x = q * 0.75f;
        float y = r * hexHeight + (q % 2 == 0 ? 0f : hexHeight / 2f);

        return new Vector3(x, y, 0);
    }
}