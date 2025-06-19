using QuantumConnect.Core.GameEngine;
using QuantumConnect.Core.Models;
using QuantumConnect.Core.Algorithms;
using System;

namespace QuantumConnect.TestConsole;

class Program
{
    static void Main()
    {
        var game = new QuantumField(rows: 8, columns: 12, winLength: 6); // Or whatever your rules are!

        var bot1 = new RandomBot();
        var bot2 = new RandomBot();

        while (game.Status == ExperimentStatus.Incomplete)
        {
            PrintBoard(game);
            int move;
            if (game.CurrentPlayer == SpaceOwnership.FirstPlayer)
            {
                move = bot1.AccelerateQuark(game);
            }
            else
            {
                move = bot2.AccelerateQuark(game);
            }
            Console.WriteLine($"{game.CurrentPlayer}: Move {move}");
            game.MakeMove(move);
        }
        PrintBoard(game);

        if (game.Status == ExperimentStatus.Collapsed)
        {
            Console.WriteLine($"Game over! Winner: {game.CurrentPlayer}");
        }
        else
        {
            Console.WriteLine("Game ended in a draw!");
        }
    }

    static void PrintBoard(QuantumField game)
    {
        for (int r = 0; r < game.Rows; r++)
        {
            for (int c = 0; c < game.Columns; c++)
            {
                var cell = game.GetCell(r, c);
                char symbol = cell switch
                {
                    SpaceOwnership.FirstPlayer => 'X',
                    SpaceOwnership.SecondPlayer => 'O',
                    _ => '.'
                };
                Console.Write(symbol);
            }
            Console.WriteLine();
        }
        Console.WriteLine(new string('-', game.Columns));
    }
}
