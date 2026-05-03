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

    public void MovePiece(Piece piece, int newX, int newY)
    {
        grid[piece.x, piece.y] = null;
        grid[newX, newY] = piece;

        piece.SetPosition(newX, newY);
    }

    public IEnumerable<Piece> GetAllPieces()
    {
        foreach (var p in grid)
            if (p != null)
                yield return p;
    }
}

