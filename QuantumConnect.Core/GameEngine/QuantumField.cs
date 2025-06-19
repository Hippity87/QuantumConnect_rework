using QuantumConnect.Core.Models;
using System.Collections.Generic;

namespace QuantumConnect.Core.GameEngine;

public class QuantumField
{
    public int Rows { get; }
    public int Columns { get; }
    private readonly SpaceOwnership[,] board;
    public SpaceOwnership CurrentPlayer { get; private set; } = SpaceOwnership.FirstPlayer;
    private readonly List<Move> moveHistory = new();
    public ExperimentStatus Status { get; private set; } = ExperimentStatus.Incomplete;

    public QuantumField(int rows = 8, int columns = 12)
    {
        Rows = rows;
        Columns = columns;
        board = new SpaceOwnership[rows, columns];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                board[r, c] = SpaceOwnership.Empty;
    }

    public SpaceOwnership GetCell(int row, int col) => board[row, col];

    public IEnumerable<Move> MoveHistory => moveHistory.AsReadOnly();

    public bool IsColumnFull(int col) => board[0, col] != SpaceOwnership.Empty;

    public bool IsBoardFull()
    {
        for (int c = 0; c < Columns; c++)
            if (!IsColumnFull(c)) return false;
        return true;
    }

    /// <summary>
    /// Tries to make a move for the current player. Returns true if move succeeds.
    /// </summary>
    public bool MakeMove(int column, bool specialMove = false)
    {
        if (Status != ExperimentStatus.Incomplete) return false;
        if (column < 0 || column >= Columns) return false;
        if (IsColumnFull(column)) return false;

        int row = -1;
        for (int r = Rows - 1; r >= 0; r--)
        {
            if (board[r, column] == SpaceOwnership.Empty)
            {
                board[r, column] = CurrentPlayer;
                row = r;
                break;
            }
        }
        if (row == -1) return false; // Should never happen

        moveHistory.Add(new Move(specialMove, column, CurrentPlayer));

        if (CheckWin(row, column))
        {
            Status = ExperimentStatus.Collapsed;
            return true;
        }
        if (IsBoardFull())
        {
            Status = ExperimentStatus.Uncertain;
            return true;
        }

        SwitchPlayer();
        return true;
    }

    private void SwitchPlayer()
    {
        CurrentPlayer = (CurrentPlayer == SpaceOwnership.FirstPlayer)
            ? SpaceOwnership.SecondPlayer
            : SpaceOwnership.FirstPlayer;
    }

    private bool CheckWin(int row, int col)
    {
        var player = board[row, col];
        int[][] directions = new int[][]
        {
            new[] {0, 1},   // horizontal
            new[] {1, 0},   // vertical
            new[] {1, 1},   // diagonal down-right
            new[] {1, -1},  // diagonal down-left
        };

        foreach (var dir in directions)
        {
            int count = 1;
            count += CountInDirection(row, col, dir[0], dir[1], player);
            count += CountInDirection(row, col, -dir[0], -dir[1], player);
            if (count >= 4) return true;
        }
        return false;
    }

    private int CountInDirection(int row, int col, int dr, int dc, SpaceOwnership player)
    {
        int count = 0;
        int r = row + dr, c = col + dc;
        while (r >= 0 && r < Rows && c >= 0 && c < Columns && board[r, c] == player)
        {
            count++;
            r += dr; c += dc;
        }
        return count;
    }

    /// <summary>
    /// Returns a copy of the board for display.
    /// </summary>
    public SpaceOwnership[,] GetBoardCopy()
    {
        var copy = new SpaceOwnership[Rows, Columns];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                copy[r, c] = board[r, c];
        return copy;
    }

    public void Reset()
    {
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Columns; c++)
                board[r, c] = SpaceOwnership.Empty;
        moveHistory.Clear();
        Status = ExperimentStatus.Incomplete;
        CurrentPlayer = SpaceOwnership.FirstPlayer;
    }
}
