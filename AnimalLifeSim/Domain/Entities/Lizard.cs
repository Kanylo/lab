
namespace AnimalLifeSim.Domain;

public class Lizard : Animal, IWalkable, ICrawlable
{
    public Lizard(string name) : base(name, "Ящірка", 2, 4, 0) { }

    public void Crawl() { if (!IsDead) TriggerEvent($"{Name} повзає по стіні."); }
    public void Walk() { if (!IsDead) TriggerEvent($"{Name} ходить."); }
}