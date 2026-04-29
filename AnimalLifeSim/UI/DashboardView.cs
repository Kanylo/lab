using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using AnimalLifeSim.Domain;
using AnimalLifeSim.Engine;

namespace AnimalLifeSim.UI;

public class DashboardView
{
    private readonly SimulationWorld _world;
    private readonly SimulationEngine _engine;
    private readonly ConcurrentQueue<string> _eventLogs = new();
    private const int MaxLogs = 5;

    // Флаг для блокування UI, коли відкрите меню
    private bool _isMenuOpen = false;

    public DashboardView(SimulationWorld world, SimulationEngine engine)
    {
        _world = world;
        _engine = engine;
        foreach (var animal in _world.Animals)
        {
            animal.DomainEvent += OnAnimalEvent;
        }
    }

    private void OnAnimalEvent(object? sender, string message)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        _eventLogs.Enqueue($"[grey]{time}[/] [bold yellow]{message}[/]");
        while (_eventLogs.Count > MaxLogs) _eventLogs.TryDequeue(out _);
    }

    public async Task RenderDashboardAsync(CancellationTokenSource cts)
    {
        while (!cts.Token.IsCancellationRequested)
        {
            if (_isMenuOpen)
            {
                // Якщо меню відкрите, просто чекаємо
                await Task.Delay(100, cts.Token);
                continue;
            }

            var table = new Table().Expand();
            UpdateTableHeaders(table);

            try
            {
                await AnsiConsole.Live(table)
                    .Overflow(VerticalOverflow.Visible)
                    .StartAsync(async ctx =>
                    {
                        while (!cts.Token.IsCancellationRequested && !_isMenuOpen)
                        {
                            PopulateTable(table);
                            
                            string logsText = _eventLogs.IsEmpty 
                                ? "[grey]Очікування дій...[/]" 
                                : string.Join("\n", _eventLogs);
                                
                            table.Caption = new TableTitle($"\n[bold white]Останні події:[/]\n{logsText}");
                            ctx.Refresh();

                            if (Console.KeyAvailable)
                            {
                                var key = Console.ReadKey(intercept: true).Key;
                                ProcessInput(key, cts);
                            }

                            await Task.Delay(100, cts.Token);
                        }
                    });
            }
            catch (TaskCanceledException) { }
        }
    }

    private void PopulateTable(Table table)
    {
        table.Rows.Clear();
        List<Animal> animals;
        lock (_world.Animals) { animals = _world.Animals.ToList(); }

        foreach (var a in animals)
        {
            string stateColor = a.IsDead ? "red" : (a.HoursSinceLastMeal > 8 ? "yellow" : "green");
            string stateStr = a.IsDead ? "Мертвий" : (a.HoursSinceLastMeal > 8 ? "Голодний" : "Норма");
            string happyStr = a.IsHappy ? "[green]Так 😃[/]" : "[red]Ні 😢[/]";

            table.AddRow(
                $"[bold cyan]{a.Name}[/] ({a.Species})",
                a.Habitat.ToString(),
                $"[{stateColor}]{stateStr}[/]",
                $"{a.HoursSinceLastMeal:F1} год",
                a.Habitat == Habitat.Wild ? "[grey]-[/]" : $"{a.HoursSinceLastCleaning:F1} год",
                happyStr,
                $"{a.MealsToday}/5"
            );
        }
    }

private void UpdateTableHeaders(Table table)
{
    // Щоб не дублювати колонки під час оновлення (запобіжник)
    if (table.Columns.Count == 0)
    {
        table.AddColumn("Ім'я / Вид");
        table.AddColumn("Локація");
        table.AddColumn("Стан");
        table.AddColumn("Час без їжі");
        table.AddColumn("Час без прибирання");
        table.AddColumn("Щасливий?");
        table.AddColumn("Прийомів їжі (сьогодні)");
    }

    // Динамічний заголовок з поточним множником часу
    table.Title = new TableTitle(
        $"[bold yellow]Симулятор Тварин[/] | [bold green]Швидкість: x{_engine.TimeScaleMultiplier}[/]\n" +
        "[grey][[F]] Годувати | [[C]] Прибрати | [[A]] Тест Активності | [[W]] На волю | [yellow][[-]][[+]][/] Час | [[Q]] Вихід[/]"
    );
}

    private void LogSystemEvent(string message)
    {
        string time = DateTime.Now.ToString("HH:mm:ss");
        _eventLogs.Enqueue($"[grey]{time}[/] [bold cyan]⚙️ {message}[/]");
        
        while (_eventLogs.Count > MaxLogs) 
            _eventLogs.TryDequeue(out _);
    }

    private void ProcessInput(ConsoleKey key, CancellationTokenSource cts)
    {
        if (key == ConsoleKey.Q) cts.Cancel();

        lock (_world.Animals)
        {
            var ownedAnimals = _world.Animals.Where(a => a.Habitat == Habitat.Owner).ToList();

            if (key == ConsoleKey.F)
            {
                _isMenuOpen = true; // Це зупинить AnsiConsole.Live
                ShowFeedMenu();     // Викликаємо синхронне меню
                _isMenuOpen = false;// Після виходу відновлюємо Live View            
            }
            if (key == ConsoleKey.C)
            {
                foreach (var a in ownedAnimals) a.Clean();
            }
            if (key == ConsoleKey.W)
            {
                foreach (var a in ownedAnimals)
                {
                    a.Habitat = Habitat.Wild;
                    // Тут ми вручну викликаємо OnAnimalEvent, бо зміна Habitat поки що не генерує подію в домені
                    OnAnimalEvent(this, $"{a.Name} відпущений(а) на волю!");
                }
            }
            if (key == ConsoleKey.A)
            {
                foreach (var a in ownedAnimals)
                {
                    if (a is IRunnable r) r.Run();
                    if (a is IFlyable f) f.Fly();
                    if (a is ICrawlable c) c.Crawl();
                }
            }
            if (key == ConsoleKey.OemPlus || key == ConsoleKey.Add)
            {
                // Обмежимо максимальну швидкість, щоб тварини не вмирали за мілісекунду
                _engine.TimeScaleMultiplier = Math.Min(_engine.TimeScaleMultiplier * 2, 5000000);
                LogSystemEvent($"Час ПРИСКОРЕНО! Множник: {_engine.TimeScaleMultiplier}");
            }
            
            if (key == ConsoleKey.OemMinus || key == ConsoleKey.Subtract)
            {
                // Не даємо часу зупинитися повністю або стати від'ємним
                _engine.TimeScaleMultiplier = Math.Max(_engine.TimeScaleMultiplier / 2, 0.5);
                LogSystemEvent($"Час УПОВІЛЬНЕНО! Множник: {_engine.TimeScaleMultiplier}");
            }
}

    }
        private void ShowFeedMenu()
    {
        AnsiConsole.Clear();

        List<Animal> hungryAnimals;
        lock (_world.Animals)
        {
            // Беремо тільки живих тварин, які належать Хазяїну
            hungryAnimals = _world.Animals
                .Where(a => a.Habitat == Habitat.Owner && !a.IsDead)
                .ToList();
        }

        if (hungryAnimals.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Немає живих тварин у Хазяїна для годування![/]");
            Thread.Sleep(2000);
            return;
        }

        // Створюємо інтерактивне меню з можливістю обрати кількох тварин
        var prompt = new MultiSelectionPrompt<Animal>()
            .Title("[green]Оберіть тварин для годування (Пробіл - вибір, Enter - підтвердити):[/]")
            .PageSize(10)
            .MoreChoicesText("[grey](Гортайте вгору/вниз)[/]")
            .InstructionsText("[grey](Натисніть [blue]<Пробіл>[/] для вибору, [green]<Enter>[/] для годування)[/]")
            .UseConverter(a => $"{a.Name} ({a.Species}) - Без їжі: {a.HoursSinceLastMeal:F1} год");

        prompt.AddChoices(hungryAnimals);

        var selectedAnimals = AnsiConsole.Prompt(prompt);

        // Годуємо обраних
        if (selectedAnimals.Count > 0)
        {
            foreach (var animal in selectedAnimals)
            {
                animal.Feed();
            }
            // Штучна затримка, щоб користувач встиг усвідомити, що відбулося
            Thread.Sleep(500); 
        }
        
        AnsiConsole.Clear(); // Очищаємо екран перед поверненням у Live View
    }
}