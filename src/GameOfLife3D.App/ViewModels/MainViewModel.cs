#nullable enable

using System;
using System.Linq;
using System.Timers;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameOfLife3D.Core;
using GameOfLife3D.Core.Rules;

namespace GameOfLife3D.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private Timer _timer = new();
    private StandardRule3D _rule;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartStopLabel))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SpeedLabel))]
    private double _stepsPerSecond = 10;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GridSizeLabel))]
    private int _gridSize = 20;

    [ObservableProperty]
    private long _generation;

    [ObservableProperty]
    private int _liveCellCount;

    [ObservableProperty]
    private string _survivalCounts = "5,6,7";

    [ObservableProperty]
    private string _birthCounts = "6";

    public string SpeedLabel => $"{StepsPerSecond:F0} steps/s";
    public string GridSizeLabel => $"{GridSize}³";
    public string StartStopLabel => IsRunning ? "Stop" : "Start";

    public SimulationEngine Engine { get; private set; }

    public event EventHandler? SimulationStepped;

    public MainViewModel()
    {
        _rule = new StandardRule3D();
        var grid = new Grid3D(GridSize, GridSize, GridSize);
        Engine = new SimulationEngine(grid, _rule);

        _timer.AutoReset = true;
        _timer.Elapsed += OnTimerElapsed;

        Engine.Randomize(0.1);
        UpdateLiveCellCount();
    }

    partial void OnStepsPerSecondChanged(double value)
    {
        _timer.Interval = 1000.0 / Math.Max(1, value);
    }

    partial void OnGridSizeChanged(int value)
    {
        bool wasRunning = IsRunning;
        if (wasRunning) StopSimulation();

        var grid = new Grid3D(value, value, value);
        Engine = new SimulationEngine(grid, _rule);
        Generation = 0;
        Engine.Randomize(0.1);
        UpdateLiveCellCount();
        SimulationStepped?.Invoke(this, EventArgs.Empty);

        if (wasRunning) StartSimulation();
    }

    partial void OnSurvivalCountsChanged(string value)
    {
        _rule.SurvivalCounts = ParseIntList(value, new[] { 5, 6, 7 });
    }

    partial void OnBirthCountsChanged(string value)
    {
        _rule.BirthCounts = ParseIntList(value, new[] { 6 });
    }

    private static int[] ParseIntList(string s, int[] fallback)
    {
        try
        {
            var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var result = parts.Select(int.Parse).ToArray();
            return result.Length > 0 ? result : fallback;
        }
        catch { return fallback; }
    }

    [RelayCommand]
    private void StartStop()
    {
        if (IsRunning) StopSimulation();
        else StartSimulation();
    }

    [RelayCommand]
    private void Step()
    {
        if (IsRunning) return;
        DoStep();
    }

    [RelayCommand]
    private void Randomize()
    {
        Engine.Randomize(0.1);
        Generation = 0;
        UpdateLiveCellCount();
        SimulationStepped?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Clear()
    {
        Engine.Clear();
        Generation = 0;
        UpdateLiveCellCount();
        SimulationStepped?.Invoke(this, EventArgs.Empty);
    }

    private void StartSimulation()
    {
        _timer.Interval = 1000.0 / Math.Max(1, StepsPerSecond);
        _timer.Start();
        IsRunning = true;
    }

    private void StopSimulation()
    {
        _timer.Stop();
        IsRunning = false;
    }

    private void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        DoStep();
    }

    private void DoStep()
    {
        Engine.Step();
        int liveCount = Engine.Grid.GetLiveCells().Count();
        Dispatcher.UIThread.Post(() =>
        {
            Generation++;
            LiveCellCount = liveCount;
        });
        SimulationStepped?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateLiveCellCount()
    {
        LiveCellCount = Engine.Grid.GetLiveCells().Count();
    }
}
