using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    public Piece[,] grid = new Piece[8, 8];

    public Piece GetPiece(int x, int y)
    {
        if (x < 0 || x >= 8 || y < 0 || y >= 8) return null;
        return grid[x, y];
    }

    public void SetPiece(int x, int y, Piece piece)
    {
        grid[x, y] = piece;
    }

    public void MovePiece(Piece piece, int x, int y)
    {
        Debug.Log($"Board updated: {piece.type} at {x},{y}");
        grid[piece.x, piece.y] = null;
        grid[x, y] = piece;

        piece.SetPosition(x, y); // ONLY grid coords
    }

    public IEnumerable<Piece> GetAllPieces()
    {
        foreach (var p in grid)
            if (p != null)
                yield return p;
    }
}
