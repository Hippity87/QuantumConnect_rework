using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Algorithms/IAlgorithm.cs
using QuantumConnect.Core.Models;

using QuantumConnect.Core.GameEngine;

namespace QuantumConnect.Core.Algorithms
{

    public interface IAlgorithm
    {
        int AccelerateQuark(QuantumField field);
        bool UseSpecialMove(QuantumField field) => false;
    }
}