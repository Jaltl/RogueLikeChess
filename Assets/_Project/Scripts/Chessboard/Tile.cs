using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class Tile : MonoBehaviour
{
    [Header("Tile Colors")]
    [SerializeField] private Color _baseColor, _offsetColor;
    [Header("Tile Renderer")]
    [SerializeField] private SpriteRenderer _renderer;
    [Header("Tile Highlights")]
    [SerializeField] private Color _highlight = new Color(186, 186, 186, 1f);
    [SerializeField] private Color _legalMoves = new Color(6, 255, 1, 1f);
    [SerializeField] private Color _capture = new Color(255, 1, 55, 1f);
    [SerializeField] private Color _latestMove = new Color(1, 101, 255, 1f);
    private Color _originalColor;
    private Color _StartColor;

    [Header("Tile Position")]
    public int x, y; // Store the tile's coordinates
    private GameController _gameController;
    public void Init(int x, int y, bool isOffset)
    {
        _renderer.color = isOffset ? _offsetColor : _baseColor;
        this.x = x;
        this.y = y;
        _gameController = FindAnyObjectByType<GameController>();
        _StartColor = _renderer.color;
    }

    private void OnMouseEnter()
    {
        _originalColor = _renderer.color;
        _renderer.color = _highlight;
    }

    private void OnMouseExit()
    {
        _renderer.color = _originalColor;
    }

    private void OnMouseDown()
    {
        Debug.Log("X: " + x + ", Y: " + y);
        _gameController.OnTileClicked(this);
    }

    public void SetlegalHighlight(bool active)
    {
        _renderer.color = _legalMoves;
    }
    public void SetCaptureHighlight(bool active)
    {
        _renderer.color = _capture;
    }

    public void SetLatestMoveHighlight(bool active)
    {
        _renderer.color = _latestMove;
    }

    public void ClearHighlight()
    {
        _renderer.color = _StartColor;
    }


}
