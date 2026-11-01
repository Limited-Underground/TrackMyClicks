// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;

namespace TrackMyClicks;

/// <summary>UI-only colors and native vector badge shapes, shared by dashboard and showcase.</summary>
public static class ExpansionPalette
{
    private static readonly Dictionary<string, SolidColorBrush> brushes = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Geometry> icons = new(StringComparer.OrdinalIgnoreCase);

    public static string Hex(string tier) => tier switch
    {
        "Bronze" => "#DCA16F", "Silver" => "#C6D4E4", "Gold" => "#FFD15B",
        "Platinum" => "#9CE5E1", "Diamond" => "#85BCFF", _ => "#27E6AF"
    };

    public static SolidColorBrush Brush(string hex)
    {
        if (brushes.TryGetValue(hex, out var brush)) return brush;
        brush = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
        brush.Freeze(); brushes.Add(hex, brush); return brush;
    }

    public static Geometry Icon(string tier)
    {
        string key = tier == "Diamond" ? "Diamond" : "Star";
        if (icons.TryGetValue(key, out var geometry)) return geometry;
        geometry = Geometry.Parse(key == "Diamond"
            ? "M 16,1 L 31,12 16,31 1,12 Z"
            : "M 16,1 L 20.5,10.4 31,11.9 23.4,19.2 25.2,29.5 16,24.6 6.8,29.5 8.6,19.2 1,11.9 11.5,10.4 Z");
        geometry.Freeze(); icons.Add(key, geometry); return geometry;
    }
}

/// <summary>Stable presentation state. Domain calculations, rewards and date formatting stay with callers.</summary>
public sealed class ExpansionCard : INotifyPropertyChanged
{
    public string Id { get; }
    private string title = "", description = "", group = "", tier = "", progressText = "", stateText = "", detailText = "";
    private double progressPercent;
    private bool isUnlocked;
    private Brush accentBrush = ExpansionPalette.Brush("#27E6AF");
    private Geometry tierGeometry = ExpansionPalette.Icon("");
    public string Title { get => title; private set => Set(ref title, value); }
    public string Description { get => description; private set => Set(ref description, value); }
    public string Group { get => group; private set => Set(ref group, value); }
    public string Tier { get => tier; private set => Set(ref tier, value); }
    public string ProgressText { get => progressText; private set => Set(ref progressText, value); }
    public string StateText { get => stateText; private set => Set(ref stateText, value); }
    public string DetailText { get => detailText; private set => Set(ref detailText, value); }
    public double ProgressPercent { get => progressPercent; private set => Set(ref progressPercent, value); }
    public bool IsUnlocked { get => isUnlocked; private set => Set(ref isUnlocked, value); }
    public Brush AccentBrush { get => accentBrush; private set => Set(ref accentBrush, value); }
    public Geometry TierGeometry { get => tierGeometry; private set => Set(ref tierGeometry, value); }
    public event PropertyChangedEventHandler? PropertyChanged;

    public ExpansionCard(string id) => Id = id;

    public void Apply(string title, string description, string group, string tier, string progressText,
        double progressPercent, bool isUnlocked, string stateText, string detailText, string accentHex)
    {
        Title = title; Description = description; Group = group; Tier = tier; ProgressText = progressText;
        ProgressPercent = double.IsFinite(progressPercent) ? Math.Clamp(progressPercent, 0, 100) : 0;
        IsUnlocked = isUnlocked; StateText = stateText; DetailText = detailText;
        AccentBrush = ExpansionPalette.Brush(accentHex); TierGeometry = ExpansionPalette.Icon(tier);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; PropertyChanged?.Invoke(this, new(property));
    }
}

public sealed class RecordCard : INotifyPropertyChanged
{
    public string Id { get; }
    public string Title { get; }
    private string valueText = "", dateText = "";
    public string ValueText { get => valueText; private set => Set(ref valueText, value); }
    public string DateText { get => dateText; private set => Set(ref dateText, value); }
    public event PropertyChangedEventHandler? PropertyChanged;
    public RecordCard(string id, string title) { Id = id; Title = title; }
    public void Apply(string valueText, string dateText) { ValueText = valueText; DateText = dateText; }
    private void Set<T>(ref T field, T value, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value; PropertyChanged?.Invoke(this, new(property));
    }
}
