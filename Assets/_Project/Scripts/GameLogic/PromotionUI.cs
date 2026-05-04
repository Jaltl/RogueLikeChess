using UnityEngine;

public class PromotionUI : MonoBehaviour
{
    public GameObject panel;
    private System.Action<PieceType> onChoice;

    public void Show(System.Action<PieceType> callback)
    {
        panel.SetActive(true);
        onChoice = callback;
    }

    public void ChooseQueen() => Select(PieceType.Queen);
    public void ChooseRook() => Select(PieceType.Rook);
    public void ChooseBishop() => Select(PieceType.Bishop);
    public void ChooseKnight() => Select(PieceType.Knight);

    void Select(PieceType type)
    {
        panel.SetActive(false);
        onChoice?.Invoke(type);
    }
}

