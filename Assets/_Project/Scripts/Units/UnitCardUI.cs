using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitCardUI : MonoBehaviour
{
    [SerializeField] private UnitDefinition unit;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button button;
    [SerializeField] private PlacementController placementController;

    private void Start()
    {
        if (unit == null)
        {
            Debug.LogError($"{name} has no UnitDefinition assigned");
            return;
        }

        if (placementController == null)
        {
            Debug.LogError($"{name} has no PlacementController assigned");
            return;
        }

        if (button == null)
        {
            Debug.LogError($"{name} has no Button assigned");
            return;
        }

        icon.sprite = unit.icon;
        nameText.text = unit.unitName;
        costText.text = unit.cost.ToString();

        button.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        Debug.Log($"Clicked card: {unit.unitName}");
        placementController.SelectUnit(unit);
    }
}