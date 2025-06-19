using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// GameEngine/QuantumField.cs
using QuantumConnect.Core.Algorithms;
using QuantumConnect.Core.Models;

namespace QuantumConnect.Core.GameEngine;

public class QuantumField
{
    public int Rows { get; }
    public int Columns { get; }

    private SpaceOwnership[,] field;

    public QuantumField(int rows = 8, int columns = 12)
    {
        Rows = rows;
        Columns = columns;
        field = new SpaceOwnership[rows, columns];
        // Initialize board to Empty
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < columns; c++)
                field[r, c] = SpaceOwnership.Empty;
    }

    public SpaceOwnership GetCell(int row, int col) => field[row, col];

    // Add logic for moves, win detection, current player, etc. here...
}
