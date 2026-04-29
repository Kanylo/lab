namespace AnimalLifeSim.Domain;

public class Canary : Animal, IWalkable, IFlyable, ISingable
{
    public Canary(string name) : base(name, "Канарка", 2, 2, 2) { }

    public void Fly() { if (CanDoActiveAction("літати")) TriggerEvent($"{Name} літає по кімнаті."); }
    public void Sing() { if (CanDoActiveAction("співати")) TriggerEvent($"{Name} гарно співає."); }
    public void Walk() { if (!IsDead) TriggerEvent($"{Name} стрибає (ходить)."); }
}