using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AnimalLifeSim.Domain;

namespace AnimalLifeSim.Engine;

public class SimulationEngine
{
    private readonly SimulationWorld _world;
    public double TimeScaleMultiplier { get; set; } 

    public SimulationEngine(SimulationWorld world, double timeScaleMultiplier)
    {
        _world = world;
        TimeScaleMultiplier = timeScaleMultiplier;
    }

    public async Task StartAsync(CancellationToken token)
    {
        DateTime lastUpdate = DateTime.Now;

        while (!token.IsCancellationRequested)
        {
            DateTime now = DateTime.Now;
            TimeSpan delta = now - lastUpdate;
            lastUpdate = now;

            // 1 реальна секунда = TimeScaleMultiplier ігрових днів
            double simulatedDaysPassed = delta.TotalDays * TimeScaleMultiplier;
            double hoursPassed = simulatedDaysPassed * 24;

            List<Animal> currentAnimals;
            lock (_world.Animals) { currentAnimals = _world.Animals.ToList(); }

            foreach (var animal in currentAnimals)
            {
                animal.Tick(hoursPassed);
            }

            await Task.Delay(10, token);
        }
    }
}