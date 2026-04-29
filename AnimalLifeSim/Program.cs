using System;
using AnimalLifeSim.Domain;
using AnimalLifeSim.Engine;
using AnimalLifeSim.UI;

var random = new Random();

// Локальна функція для генерації випадкової локації (тільки Owner або PetShop)
Habitat GetRandomStartingHabitat()
{
    return random.Next(0, 2) == 0 ? Habitat.Owner : Habitat.PetShop;
}

var world = new SimulationWorld();
world.Animals.Add(new Dog("Рекс") { Habitat = GetRandomStartingHabitat() });
world.Animals.Add(new Canary("Кеша") { Habitat = GetRandomStartingHabitat() });
world.Animals.Add(new Lizard("Годзилла") { Habitat = GetRandomStartingHabitat() });
// Давай додамо ще кілька для масовки
world.Animals.Add(new Dog("Барбос") { Habitat = GetRandomStartingHabitat() });
world.Animals.Add(new Canary("Твіті") { Habitat = GetRandomStartingHabitat() });

var engine = new SimulationEngine(world, 2.0); // 1 сек = 2 дні
var cts = new CancellationTokenSource();
var view = new DashboardView(world, engine);

Task.Run(() => engine.StartAsync(cts.Token));
await view.RenderDashboardAsync(cts);