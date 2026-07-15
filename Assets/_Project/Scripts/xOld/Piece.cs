// using System.Collections.Generic;
// using UnityEngine;
// public enum PieceType
// {
//     Pawn = 1,
//     Knight = 2,
//     Bishop = 3,
//     Rook = 4,
//     Queen = 5,
//     King = 6
// }

// public class Piece : MonoBehaviour
// {
//     public PieceType type;
//     public int x, y;
//     public bool isWhite; // true for white pieces, false for black pieces

//     public bool hasMoved = false; // Track if the piece has moved (important for castling and pawn's first move)


//     public void SetPosition(int x, int y)
//     {
//         this.x = x;
//         this.y = y;
//     }

//     public void BlackTeam()
//     {
//         SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
//         sr.material.color = Color.orange;
        
//     }
//     //private void OnEnable()
//     //{
//     //    if (isWhite)
//     //    {
//     //        name = $"White_{(int)transform.position.x}_{(int)transform.position.y}";
//     //        print($"White piece initialized at position: {transform.position}");
//     //    }
//     //    else
//     //    {
//     //        name = $"Black_{(int)transform.position.x}_{(int)transform.position.y}";
//     //        pieceValue *= -1; // Negate the value for black pieces
//     //        print($"Black piece initialized at position: {transform.position}");
//     //    }
//     //}
// }
