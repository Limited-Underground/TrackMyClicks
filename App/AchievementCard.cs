// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TrackMyClicks.Core;
using Brush=System.Windows.Media.Brush;
namespace TrackMyClicks;

/// <summary>Legacy preview presentation; all thresholds come from the central domain catalog.</summary>
public sealed class AchievementCard : INotifyPropertyChanged
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    private double progressPercent;
    private string progressText="",percentText="",tierText="";
    private bool isUnlocked;
    private Brush accentBrush=ExpansionPalette.Brush("#27E6AF");
    public double ProgressPercent { get=>progressPercent; private set=>Set(ref progressPercent,value); }
    public string ProgressText { get=>progressText; private set=>Set(ref progressText,value); }
    public string PercentText { get=>percentText; private set=>Set(ref percentText,value); }
    public string TierText { get=>tierText; private set=>Set(ref tierText,value); }
    public Brush AccentBrush { get=>accentBrush; private set=>Set(ref accentBrush,value); }
    public bool IsUnlocked { get=>isUnlocked; private set=>Set(ref isUnlocked,value); }
    public event PropertyChangedEventHandler? PropertyChanged;

    public AchievementCard(Achievement achievement)
    {
        Id=achievement.Id; Title=achievement.Title; Description=achievement.Description;
    }
    public void Update(ExpandedAchievement achievement)
        =>Apply(achievement.Unlocked,achievement.Current,achievement.Target,achievement.Tier);
    // Compatibility with the V0.2 presentation regression suite; no threshold is inferred from an ID.
    public void Update(Achievement achievement,Totals lifetime,double pixelsPerInch)
    {
        var definition=ExpansionRules.Achievements.Single(d=>d.Id==Id);
        double current=definition.Metric switch
        {
            InsightMetric.Clicks=>lifetime.Clicks,
            InsightMetric.Keyboard=>lifetime.KeyPresses,
            InsightMetric.MovementMeters=>lifetime.DistancePixels/pixelsPerInch*.0254,
            InsightMetric.Scroll=>lifetime.ScrollUnits,
            _=>0
        };
        Apply(achievement.Unlocked,current,definition.Target,definition.Tier);
    }
    private void Apply(bool unlocked,double current,double target,AchievementTier tier)
    {
        IsUnlocked=unlocked;
        ProgressPercent=unlocked?100:Math.Clamp(current/target*100,0,100);
        PercentText=unlocked?"Completed":$"{Math.Floor(ProgressPercent):N0}%";
        ProgressText=unlocked?$"{target:N0} / {target:N0}":$"{Math.Min(current,target):N0} / {target:N0}";
        TierText=tier.ToString();
        AccentBrush=ExpansionPalette.Brush(ExpansionPalette.Hex(TierText));
    }
    private void Set<T>(ref T field,T value,[CallerMemberName]string? property=null)
    {
        if(EqualityComparer<T>.Default.Equals(field,value))return;
        field=value; PropertyChanged?.Invoke(this,new(property));
    }
}
