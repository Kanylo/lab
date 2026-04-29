namespace AnimalLifeSim.Domain;

public class Dog : Animal, IRunnable, IWalkable
{
    public Dog(string name) : base(name, "Собака", 2, 4, 0) { }

    public void Run() { if (CanDoActiveAction("бігати")) TriggerEvent($"{Name} радісно бігає."); }
    public void Walk() { if (!IsDead) TriggerEvent($"{Name} повільно ходить."); }
}