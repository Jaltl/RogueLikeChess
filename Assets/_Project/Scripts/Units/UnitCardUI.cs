using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitCardUI : MonoBehaviour
{
    [Header("Only Required Reference")]
    [SerializeField] private UnitDefinition unitDefinition;

    [Header("Auto Found")]
    [SerializeField] private TrianglePlacementController placementController;
    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text powerText;
    [SerializeField] private TMP_Text healthText;

    private void Reset()
    {
        AutoWire();
        Refresh();
    }

    private void OnValidate()
    {
        AutoWire();
        Refresh();
    }

    private void Awake()
    {
        AutoWire();

        if (button != null)
        {
            button.onClick.RemoveListener(SelectUnit);
            button.onClick.AddListener(SelectUnit);
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(SelectUnit);
    }

    public void SetUnitDefinition(UnitDefinition definition)
    {
        unitDefinition = definition;
        Refresh();
    }

    public void SelectUnit()
    {
        if (unitDefinition == null)
        {
            Debug.LogWarning($"{name} has no UnitDefinition assigned.");
            return;
        }

        if (placementController == null)
            placementController = FindFirstObjectByType<TrianglePlacementController>();

        if (placementController == null)
        {
            Debug.LogWarning("No TrianglePlacementController found in scene.");
            return;
        }

        placementController.SelectUnit(unitDefinition);
    }

    private void Refresh()
    {
        if (unitDefinition == null)
            return;

        gameObject.name = $"Unit Card - {unitDefinition.DisplayName}";

        if (nameText != null)
            nameText.text = unitDefinition.DisplayName;

        if (costText != null)
            costText.text = unitDefinition.cost.ToString();

        if (powerText != null)
            powerText.text = unitDefinition.power.ToString();

        if (healthText != null)
            healthText.text = unitDefinition.health.ToString();

        if (iconImage != null)
        {
            iconImage.sprite = unitDefinition.unitIcon;
            iconImage.enabled = unitDefinition.unitIcon != null;
        }
    }

    private void AutoWire()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (placementController == null && Application.isPlaying)
            placementController = FindFirstObjectByType<TrianglePlacementController>();

        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        foreach (TMP_Text text in texts)
        {
            string lower = text.gameObject.name.ToLower();

            if (nameText == null && lower.Contains("name"))
                nameText = text;
            else if (costText == null && lower.Contains("cost"))
                costText = text;
            else if (powerText == null && lower.Contains("power"))
                powerText = text;
            else if (healthText == null && lower.Contains("health"))
                healthText = text;
        }

        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image.gameObject == gameObject)
                continue;

            string lower = image.gameObject.name.ToLower();

            if (iconImage == null &&
                (lower.Contains("icon") || lower.Contains("portrait") || lower.Contains("art") || lower.Contains("sprite")))
            {
                iconImage = image;
                return;
            }
        }
    }
}