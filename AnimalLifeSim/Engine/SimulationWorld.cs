using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimalLifeSim.Domain;

namespace AnimalLifeSim.Engine;

public class SimulationWorld
{
    public List<Animal> Animals { get; } = new();
}