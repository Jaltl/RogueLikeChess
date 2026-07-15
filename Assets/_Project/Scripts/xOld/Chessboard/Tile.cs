// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class Tile : MonoBehaviour
// {
//     [Header("Tile Colors")]
//     [SerializeField] private Color _baseColor, _offsetColor;
//     [Header("Tile Renderer")]
//     [SerializeField] private SpriteRenderer _renderer;
//     [Header("Tile Highlights")]
//     [SerializeField] private GameObject glow;
//     [SerializeField] private SpriteRenderer glowRenderer;
//     private Material glowMaterial;
//     [SerializeField] private SpriteRenderer HoverRenderer;
//     private Material HoverMaterial;
//     [SerializeField] private SpriteRenderer CheckRenderer;

//     [Header("Tile Position")]
//     public int x, y; // Store the tile's coordinates
//     private GameController _gameController;
//     private bool isHovered;
//     private static string glowIntensity = "_Intensity";
//     private static string glowColor = "_GlowColor";

//     private bool isMove = false;
//     private bool isCapture = false;
//     private bool isLastMove = false;
//     private Color _StartColor;


//     public void Init(int x, int y, bool isOffset)
//     {
//         _renderer.color = isOffset ? _offsetColor : _baseColor;
//         _StartColor = _renderer.color;
//         this.x = x;
//         this.y = y;
//         _gameController = FindAnyObjectByType<GameController>();
//         //glowMaterial.SetColor("_GlowColor", Color.green);
//         glowMaterial = glowRenderer.material;
//         glowRenderer.enabled = true;
//         glowMaterial.SetFloat(glowIntensity, 1f);
//         HoverMaterial = HoverRenderer.material;
//         HoverMaterial.SetFloat(glowIntensity, 1f);
//         HoverMaterial.SetColor(glowColor, Color.darkGray);
//         HoverRenderer.enabled = false;
//         CheckRenderer.enabled = false;
//     }

//     private void OnMouseEnter()
//     {
//         HoverRenderer.enabled = true;
//     }

//     private void OnMouseExit()
//     {
//         HoverRenderer.enabled = false;
//         //StopPulse();
//     }

//     //public void SetHover(bool Hovered)
//     //{
//     //    isHovered = Hovered;
//     //    if (isHovered)
//     //        SetGlow(color: Color.yellow, intensity: 1f);

//     //    else
//     //        StopPulse();
//     //}

//     private void OnMouseDown()
//     {
//         //Debug.Log("X: " + x + ", Y: " + y);
//         _gameController.OnTileClicked(this);
//     }

//     public void ShowMove(bool active)
//     {
//         isMove = active;
//         UpdateColor();
//     }

//     public void ShowCapture(bool active)
//     {
//         isCapture = active;
//         UpdateColor();
//     }

//     public void ShowLastMove(bool active)
//     {
//         //Debug.Log($"made blue {active}");
//         isLastMove = active;
//         UpdateColor();
//     }

//     //public void HideGlow()
//     //{
//     //    StopPulse();
//     //    SetGlow(color: Color.black, intensity: 0f);
//     //}

//     //private Coroutine pulseRoutine;

//     //public void StartPulse(Color color)
//     //{
//     //    StopPulse();
//     //    pulseRoutine = StartCoroutine(GlowPulse(color));
//     //}

//     //public void StopPulse()
//     //{
//     //    if (pulseRoutine != null)
//     //        StopCoroutine(pulseRoutine);

//     //    glowMaterial.SetFloat(glowIntensity, 0f);
//     //    glowRenderer.enabled = false;
//     //}

//     //private void SetGlow(Color color, float intensity)
//     //{
//     //    glowRenderer.enabled = true;
//     //    glowMaterial.SetColor(glowColor, color);
//     //    //glowMaterial.SetFloat(glowIntensity, intensity);
//     //}

//     //private IEnumerator GlowPulse(Color color)
//     //{
//     //    glowMaterial.SetColor(glowColor, color);

//     //    float t = 0;

//     //    while (true)
//     //    {
//     //        t += Time.deltaTime * 4f;

//     //        float intensity = 1f + Mathf.Sin(t) * 0.5f;

//     //        glowMaterial.SetFloat(glowIntensity, intensity);

//     //        yield return null;
//     //    }
//     //}

//     public void ClearAllHighlight()
//     {
//         isMove = false;
//         isCapture = false;
//         UpdateColor();
//     }

//     public void KingInCheck()
//     {
//         CheckRenderer.enabled = true;
//     }

//     public void ClearCheck()
//     {
//         CheckRenderer.enabled = false;
//     }

//     public IEnumerator KingPinFlash(Piece king)
//     {
//         SpriteRenderer sr = king.GetComponent<SpriteRenderer>();

//         for (int i = 0; i < 3; i++)
//         {
//             sr.color = Color.red;
//             yield return new WaitForSeconds(0.1f);

//             sr.color = Color.white;
//             yield return new WaitForSeconds(0.1f);
//         }
//     }

//     private void UpdateColor()
//     {
//         if (!isLastMove && !isMove && !isCapture)
//         {
//             _renderer.color = _StartColor;
//         }
        
//         else if (isLastMove)
//             _renderer.color = Color.blue;
//         else if (isCapture)
//             _renderer.color = Color.red;
//         else if (isMove)
//             _renderer.color = Color.green;
//     }

// }
