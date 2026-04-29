using System;

namespace AnimalLifeSim.Domain;

public abstract class Animal
{
    public string Name { get; }
    public string Species { get; }
    public int Eyes { get; }
    public int Paws { get; }
    public int Wings { get; }

    public double HoursLived { get; private set; }
    public double HoursSinceLastMeal { get; private set; }
    public double HoursSinceLastCleaning { get; private set; }
    public int MealsToday { get; private set; }
    
    public bool IsDead { get; private set; }
    public Habitat Habitat { get; set; }

    // Щастя: або живе на волі, або прибирали не більше 24 год тому
    public bool IsHappy => Habitat == Habitat.Wild || HoursSinceLastCleaning <= 24;

    // Подія для розв'язки (Decoupling) UI від Домену
    public event EventHandler<string> DomainEvent;

    protected Animal(string name, string species, int eyes, int paws, int wings)
    {
        Name = name;
        Species = species;
        Eyes = eyes;
        Paws = paws;
        Wings = wings;
        Habitat = Habitat.PetShop; // За замовчуванням
    }

    public void Feed()
    {
        if (IsDead) return;
        MealsToday++;
        HoursSinceLastMeal = 0;
        TriggerEvent($"{Name} ({Species}) поїв(ла).");
        
        if (MealsToday > 5) Die("Переїдання (більше 5 разів на день)");
    }

    public void Clean()
    {
        if (IsDead || Habitat == Habitat.Wild) return;
        HoursSinceLastCleaning = 0;
        TriggerEvent($"У {Name} прибрано.");
    }

    public void Tick(double hoursPassed)
    {
        if (IsDead) return;

        double previousHours = HoursLived;
        HoursLived += hoursPassed;
        HoursSinceLastMeal += hoursPassed;
        HoursSinceLastCleaning += hoursPassed;

        // Вмирання від голоду (n годин без їжі)
        if (HoursSinceLastMeal > 8)
        {
            Die("Голод (більше 8 год без їжі)");
            return;
        }

        // Перевірка початку нової доби (кожні 24 години)
        int currentDay = (int)(HoursLived / 24);
        int previousDay = (int)(previousHours / 24);

        if (currentDay > previousDay)
        {
            if (MealsToday < 1)
            {
                Die("Голод (жодного прийому їжі за добу)");
                return;
            }
            MealsToday = 0; // Скидаємо лічильник для нової доби
        }
    }

    private void Die(string reason)
    {
        IsDead = true;
        TriggerEvent($"💀 {Name} ({Species}) помер(ла). Причина: {reason}");
    }

    protected void TriggerEvent(string message) => DomainEvent?.Invoke(this, message);

    // Допоміжний метод для активних дій (бігати, літати, співати)
    protected bool CanDoActiveAction(string actionName)
    {
        if (IsDead) return false;
        if (HoursSinceLastMeal > 8)
        {
            TriggerEvent($"{Name} занадто голодний(а) щоб {actionName}. (Минуло >8 год)");
            return false;
        }
        return true;
    }
}