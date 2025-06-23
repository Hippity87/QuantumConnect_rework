using QuantumConnect.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuantumConnect.Core.GameEngine
{
    public class QuantumField
    {
        public int Rows { get; }
        public int Columns { get; }
        public int WinLength { get; }
        private readonly SpaceOwnership[,] board;
        public SpaceOwnership CurrentPlayer { get; private set; } = SpaceOwnership.FirstPlayer;
        public ExperimentStatus Status { get; private set; } = ExperimentStatus.Incomplete;
        private readonly List<Move> moveHistory = new();
        public IEnumerable<Move> MoveHistory => moveHistory.AsReadOnly();

        // Bomb/cooldown logic
        private int bombCooldownFirst = 0;
        private int bombCooldownSecond = 0;
        public int BombCooldown =>
            (CurrentPlayer == SpaceOwnership.FirstPlayer) ? bombCooldownFirst : bombCooldownSecond;

        private readonly List<(int row, int col)> lastBombBlast = new();
        public IReadOnlyList<(int row, int col)> LastBombBlast => lastBombBlast;

        public QuantumField(int rows = 8, int columns = 12, int winLength = 6)
        {
            Rows = rows;
            Columns = columns;
            WinLength = winLength;
            board = new SpaceOwnership[rows, columns];
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                    board[r, c] = SpaceOwnership.Empty;
        }

        public SpaceOwnership GetCell(int row, int col) => board[row, col];

        public bool IsSpaceEmpty(int row, int col)
        {
            if (row < 0 || row >= Rows || col < 0 || col >= Columns)
                return false;
            return board[row, col] == SpaceOwnership.Empty;
        }

        public bool BelongsToMe(int row, int col)
        {
            // Assumes bot is always the CurrentPlayer when asking
            return board[row, col] == CurrentPlayer;
        }

        public SpaceOwnership[,] GetField()
        {
            var copy = new SpaceOwnership[Rows, Columns];
            Array.Copy(board, copy, board.Length);
            return copy;
        }

        public bool IsColumnFull(int col) => board[0, col] != SpaceOwnership.Empty;

        public bool IsBoardFull()
        {
            for (int c = 0; c < Columns; c++)
                if (!IsColumnFull(c)) return false;
            return true;
        }

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
            if (row == -1) return false;

            moveHistory.Add(new Move(specialMove, column, CurrentPlayer));
            if (CheckWin(row, column))
                Status = ExperimentStatus.Collapsed;
            else if (IsBoardFull())
                Status = ExperimentStatus.Uncertain;
            else
                SwitchPlayer();


            return true;
        }

        /// <summary>
        /// Use a bomb in the given column (right-click). Affects a 3x3 grid centered on bomb impact, applies gravity, sets cooldown.
        /// Returns true if successful.
        /// </summary>
        public bool BombMove(int column)
        {
            if (Status != ExperimentStatus.Incomplete) return false;
            if (column < 0 || column >= Columns) return false;
            if (BombCooldown > 0) return false;

            // Find the lowest empty spot in the column
            int bombRow = -1;
            for (int row = Rows - 1; row >= 0; row--)
            {
                if (board[row, column] == SpaceOwnership.Empty)
                {
                    bombRow = row;
                    break;
                }
            }
            if (bombRow == -1) return false; // column full; cannot bomb here


            // Track which cells are hit (for animation)
            lastBombBlast.Clear();

            // Blast radius: 3x3
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    int rr = bombRow + dr;
                    int cc = column + dc;
                    if (rr >= 0 && rr < Rows && cc >= 0 && cc < Columns)
                    {
                        if (board[rr, cc] != SpaceOwnership.Empty)
                        {
                            board[rr, cc] = SpaceOwnership.Empty;
                            lastBombBlast.Add((rr, cc));
                        }
                    }
                }
            }

            // Gravity: after bomb
            ApplyGravity();


            // Check win condition after bomb
            if (CheckAnyWin())
            {
                Status = ExperimentStatus.Collapsed;
                return true;
            }
            else if (IsBoardFull())
            {
                Status = ExperimentStatus.Uncertain;
                return true;
            }

            // Set bomb cooldown (10 turns)
            if (CurrentPlayer == SpaceOwnership.FirstPlayer)
                bombCooldownFirst = 10;
            else
                bombCooldownSecond = 10;


            // Move to next player, clear any game over
            SwitchPlayer();

            // Bomb ends the turn
            return true;
        }

        /// <summary>
        /// Move all discs down to fill empty spots below after bomb/clear.
        /// </summary>
        private void ApplyGravity()
        {
            for (int col = 0; col < Columns; col++)
            {
                int writeRow = Rows - 1;
                for (int row = Rows - 1; row >= 0; row--)
                {
                    if (board[row, col] != SpaceOwnership.Empty)
                    {
                        if (writeRow != row)
                        {
                            board[writeRow, col] = board[row, col];
                            board[row, col] = SpaceOwnership.Empty;
                        }
                        writeRow--;
                    }
                }
            }
        }

        /// <summary>
        /// Call this in your UI after bomb animation ends (e.g. after 300ms).
        /// </summary>
        public void ClearBombBlast()
        {
            lastBombBlast.Clear();
        }

        public void Reset()
        {
            for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                    board[r, c] = SpaceOwnership.Empty;
            moveHistory.Clear();
            Status = ExperimentStatus.Incomplete;
            CurrentPlayer = SpaceOwnership.FirstPlayer;
            bombCooldownFirst = 0;
            bombCooldownSecond = 0;
            lastBombBlast.Clear();
        }

        public void SwitchPlayer()
        {
            CurrentPlayer = (CurrentPlayer == SpaceOwnership.FirstPlayer)
                ? SpaceOwnership.SecondPlayer
                : SpaceOwnership.FirstPlayer;

            // Only decrement cooldown if not 0 and not just set by bomb
            if (CurrentPlayer == SpaceOwnership.FirstPlayer && bombCooldownFirst > 0)
                bombCooldownFirst--;
            else if (CurrentPlayer == SpaceOwnership.SecondPlayer && bombCooldownSecond > 0)
                bombCooldownSecond--;
        }

        // Win logic (connect WinLength)

        private bool CheckAnyWin()
        {
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Columns; c++)
                {
                    if (board[r, c] != SpaceOwnership.Empty)
                    {
                        if (CheckWin(r, c))
                            return true;
                    }
                }
            }
            return false;
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
                if (count >= WinLength) return true;
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
    }
}
