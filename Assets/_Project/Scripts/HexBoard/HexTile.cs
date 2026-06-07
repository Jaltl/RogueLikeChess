using UnityEngine;

public class HexTile : MonoBehaviour
{
    public Vector2Int axial; // x = q, y = r

    [SerializeField] private GameObject placementHighlight;
    [SerializeField] private GameObject hoverHighlight;

    private PlacementController placementController;

    public void Init(Vector2Int axial, PlacementController controller)
    {
        this.axial = axial;
        placementController = controller;

        SetPlacementHighlight(false);
        SetHover(false);
    }

    private void OnMouseDown()
    {
        placementController.OnHexClicked(this);
    }

    private void OnMouseEnter()
    {
        SetHover(true);
    }

    private void OnMouseExit()
    {
        SetHover(false);
    }

    public void SetPlacementHighlight(bool active)
    {
        placementHighlight.SetActive(active);
    }

    public void SetHover(bool active)
    {
        hoverHighlight.SetActive(active);
    }
}