using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Tile Colors")]
    [SerializeField] private Color _baseColor, _offsetColor;
    [Header("Tile Renderer")]
    [SerializeField] private SpriteRenderer _renderer;
    [Header("Tile Highlights")]
    [SerializeField] private GameObject glow;
    [SerializeField] private SpriteRenderer glowRenderer;
    private Material glowMaterial;

    [Header("Tile Position")]
    public int x, y; // Store the tile's coordinates
    private GameController _gameController;
    private bool isHovered;
    private static string glowIntensity = "_Intensity";
    private static string glowColor = "_GlowColor";
    public void Init(int x, int y, bool isOffset)
    {
        _renderer.color = isOffset ? _offsetColor : _baseColor;
        this.x = x;
        this.y = y;
        _gameController = FindAnyObjectByType<GameController>();
        //glowMaterial.SetColor("_GlowColor", Color.green);
        glowMaterial = glowRenderer.material;
        glowRenderer.enabled = true;
        glowMaterial.SetFloat(glowIntensity, 0f);
    }

    private void OnMouseEnter()
    {
        SetHover(true);
        //UpdateColor();
    }

    private void OnMouseExit()
    {
        SetHover(false);
       // UpdateColor();
    }

    public void SetHover(bool Hovered)
    {
        isHovered = Hovered;
        if (isHovered)
            SetGlow(color: Color.yellow, intensity: 1f);

        else
            StopPulse();
    }

    private void OnMouseDown()
    {
        Debug.Log("X: " + x + ", Y: " + y);
        _gameController.OnTileClicked(this);
    }

    public void ShowMove()
    {
        SetGlow(color: Color.green, intensity: 1.2f);
    }

    public void ShowCapture()
    {
        SetGlow(color: Color.red, intensity: 1.8f);
    }

    public void ShowLastMove()
    {
        SetGlow(color: Color.blue, intensity: 1.0f);
    }

    public void HideGlow()
    {
        StopPulse();
        SetGlow(color: Color.black, intensity: 0f);
    }

    private Coroutine pulseRoutine;

    public void StartPulse(Color color)
    {
        StopPulse();
        pulseRoutine = StartCoroutine(GlowPulse(color));
    }

    public void StopPulse()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);

        glowMaterial.SetFloat(glowIntensity, 0f);
        glowRenderer.enabled = false;
    }

    private void SetGlow(Color color, float intensity)
    {
        glowMaterial.SetColor(glowColor, color);
        glowMaterial.SetFloat(glowIntensity, intensity);
    }

    private IEnumerator GlowPulse(Color color)
    {
        glowMaterial.SetColor(glowColor, color);

        float t = 0;

        while (true)
        {
            t += Time.deltaTime * 4f;

            float intensity = 1f + Mathf.Sin(t) * 0.5f;

            glowMaterial.SetFloat(glowIntensity, intensity);

            yield return null;
        }
    }

    //public void ClearAllHighlight()
    //{
    //    isMove = false;
    //    isCapture = false;
    //    isLastMove = false;
    //    UpdateColor();
    //}

    //private void UpdateColor()
    //{
    //    if (isLastMove)
    //        _renderer.color = _latestMove;
    //    else if (isCapture)
    //        _renderer.color = _capture;
    //    else if (isMove)
    //        _renderer.color = _legalMoves;
    //    else if (isHovered)
    //        _renderer.color = _highlight;
    //    else
    //        _renderer.color = _StartColor;
    //}

}
