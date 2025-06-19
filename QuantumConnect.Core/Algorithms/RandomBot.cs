using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantumConnect.Core.Algorithms;
using QuantumConnect.Core.GameEngine;
using QuantumConnect.Core.Models;


namespace QuantumConnect.Core.Algorithms {

public class RandomBot : IAlgorithm
{
    private readonly Random rng = new();

    public int AccelerateQuark(QuantumField field)
    {
        var validColumns = new List<int>();
        for (int c = 0; c < field.Columns; c++)
            if (!field.IsColumnFull(c)) validColumns.Add(c);

        return validColumns.Count > 0
            ? validColumns[rng.Next(validColumns.Count)]
            : 0; // fallback to first column if all are full (shouldn't happen)
    }

    public bool UseSpecialMove(QuantumField field) => false; // implement as needed
}
}