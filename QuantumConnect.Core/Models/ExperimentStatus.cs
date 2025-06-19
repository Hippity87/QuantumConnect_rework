using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/ExperimentStatus.cs
namespace QuantumConnect.Core.Models;

public enum ExperimentStatus
{
    Incomplete,
    Collapsed,
    Uncertain
}
