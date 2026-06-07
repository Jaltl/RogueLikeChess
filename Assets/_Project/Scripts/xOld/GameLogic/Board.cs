using System.Collections.Generic;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private GameController gameController;
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
        // SAFETY: clear old position
        if (grid[piece.x, piece.y] == piece)
            grid[piece.x, piece.y] = null;

        // SAFETY: overwrite target
        grid[x, y] = piece;

        piece.SetPosition(x, y);
    }

    public IEnumerable<Piece> GetAllPieces()
    {
        foreach (var p in grid)
            if (p != null)
                yield return p;
    }

    public void RemovePiece(Piece piece)
    {
        if (piece == null) return;

        if (grid[piece.x, piece.y] == piece)
            grid[piece.x, piece.y] = null;
    }
}
