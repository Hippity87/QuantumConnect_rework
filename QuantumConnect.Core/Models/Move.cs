using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Models/Move.cs
namespace QuantumConnect.Core.Models;

public record Move(bool IsSpecialMove, int Column, SpaceOwnership Player);
