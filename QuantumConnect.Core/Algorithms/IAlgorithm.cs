using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


// Algorithms/IAlgorithm.cs
using QuantumConnect.Core.Models;

namespace QuantumConnect.Core.Algorithms;

public interface IAlgorithm
{
    int AccelerateQuark(QuantumField field);
    bool UseSpecialMove(QuantumField field) => false;
}
