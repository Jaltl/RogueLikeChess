using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitCardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TrianglePlacementController placementController;
    [SerializeField] private UnitDefinition unitDefinition;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text anchorText;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(SelectUnit);
    }

    private void Start()
    {
        Refresh();
    }

    public void Initialize(UnitDefinition definition, TrianglePlacementController controller)
    {
        unitDefinition = definition;
        placementController = controller;

        Refresh();
    }

    public void SelectUnit()
    {
        if (placementController == null || unitDefinition == null)
            return;

        placementController.SelectUnit(unitDefinition);
    }

    void Refresh()
    {
        if (unitDefinition == null)
            return;

        if (iconImage != null)
            iconImage.sprite = unitDefinition.unitIcon;

        if (nameText != null)
            nameText.text = unitDefinition.unitName;

        if (powerText != null)
            powerText.text = unitDefinition.power.ToString();

        if (costText != null)
            costText.text = unitDefinition.cost.ToString();

        if (anchorText != null)
            anchorText.text = GetAnchorLabel(unitDefinition.anchorType);
    }

    string GetAnchorLabel(UnitAnchorType anchorType)
    {
        switch (anchorType)
        {
            case UnitAnchorType.Corner:
                return "Corner";

            case UnitAnchorType.SideMidpoint:
                return "Side";

            case UnitAnchorType.TriangleCenter:
            default:
                return "Center";
        }
    }
}