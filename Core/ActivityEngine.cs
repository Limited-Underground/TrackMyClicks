// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
namespace TrackMyClicks.Core;

public enum MouseButton { Left, Right, Middle }
public sealed class Totals
{
    public long LeftClicks { get; set; }
    public long RightClicks { get; set; }
    public long MiddleClicks { get; set; }
    public long KeyPresses { get; set; }
    public double DistancePixels { get; set; }
    public double ScrollUnits { get; set; }
    public long Clicks => LeftClicks + RightClicks + MiddleClicks;
}
public sealed record Achievement(string Id, string Title, string Description, bool Unlocked);

public sealed class ActivityEngine
{
    private readonly Store store;
    private readonly Func<DateTime> clock;
    private readonly HashSet<string> unlocked;
    private string day;
    public Totals Today { get; private set; }
    public Totals Lifetime { get; }
    public ActivityInsights Insights { get; }
    public bool Paused { get; set; }
    public bool NotificationsEnabled { get; set; } = true;
    public bool Metric { get; set; }
    public double PixelsPerInch { get; set; } = 96;
    public event Action<Achievement>? AchievementUnlocked;
    private static readonly (string Id, string Title, string Description, double Threshold, int Kind)[] Milestones =
    [ ("click100", "First century", "100 mouse clicks", 100, 0), ("click1000", "Click collector", "1,000 mouse clicks", 1000, 0),
      ("click10000", "Click champion", "10,000 mouse clicks", 10000, 0), ("key1000", "Keyboard warm-up", "1,000 key presses", 1000, 1),
      ("key10000", "Key explorer", "10,000 key presses", 10000, 1), ("key100000", "Keyboard marathon", "100,000 key presses", 100000, 1),
      ("move100", "On the move", "100 meters of estimated pointer travel", 100 * 96 / .0254, 2),
      ("move1000", "Pointer kilometer", "1 kilometer of estimated pointer travel", 1000 * 96 / .0254, 2),
      ("move10000", "Long-distance pointer", "10 kilometers of estimated pointer travel", 10000 * 96 / .0254, 2) ];
    public IReadOnlyList<Achievement> Achievements => Milestones.Select(m => new Achievement(m.Id,m.Title,m.Description,unlocked.Contains(m.Id))).ToArray();
    public ActivityEngine(Store store, Func<DateTime>? clock = null, Func<double>? rateClock = null)
    {
        this.store = store; this.clock = clock ?? (() => DateTime.Now); day = this.clock().ToString("yyyy-MM-dd");
        Today = store.ReadTotals(day); Lifetime = store.ReadTotals("lifetime"); unlocked = store.ReadAchievements();
        Paused = store.ReadSetting("paused") == "1";
        NotificationsEnabled = store.ReadSetting("notifications") != "0"; Metric = store.ReadSetting("metric") == "1";
        if (double.TryParse(store.ReadSetting("ppi"),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double ppi) && ppi > 0 && double.IsFinite(ppi)) PixelsPerInch=ppi;
        Insights=new ActivityInsights(store,this.clock,day,Today,Lifetime,PixelsPerInch,rateClock);
    }
    public void RefreshDay()
    {
        string current = clock().ToString("yyyy-MM-dd");
        if (current == day) return;
        Save(); day = current; Today = store.ReadTotals(day);
        Insights.UpdateContext(day,Today,Lifetime,PixelsPerInch);
    }
    private bool CanTrack() { RefreshDay(); return !Paused; }
    public void RefreshInsights() { RefreshDay(); Insights.UpdateContext(day,Today,Lifetime,PixelsPerInch); }
    public void Click(MouseButton button,double? observedSeconds=null)
    {
        if (!CanTrack() || !Enum.IsDefined(button)) return;
        foreach (var t in new[] { Today, Lifetime })
            switch (button) { case MouseButton.Left: t.LeftClicks++; break; case MouseButton.Right: t.RightClicks++; break; case MouseButton.Middle: t.MiddleClicks++; break; }
        ObserveInsights(ActivityKind.Click,observedSeconds); CheckAchievements(); CommitInsights();
    }
    public void KeyPress() { if (!CanTrack()) return; Today.KeyPresses++; Lifetime.KeyPresses++; ObserveInsights(ActivityKind.Keyboard); CheckAchievements(); CommitInsights(); }
    public void Move(double pixels) { if (!CanTrack() || !double.IsFinite(pixels) || pixels <= 0) return; Today.DistancePixels += pixels; Lifetime.DistancePixels += pixels; ObserveInsights(ActivityKind.Movement); CheckAchievements(); CommitInsights(); }
    public void Scroll(int delta) { if (!CanTrack()) return; double amount = Math.Abs((double)delta) / 120; Today.ScrollUnits += amount; Lifetime.ScrollUnits += amount; if(delta!=0) { ObserveInsights(ActivityKind.Scroll); CommitInsights(); } }
    private void CheckAchievements()
    {
        foreach (var m in Milestones)
        {
            double value = m.Kind switch { 0 => Lifetime.Clicks, 1 => Lifetime.KeyPresses, _ => Lifetime.DistancePixels };
            double threshold = m.Kind == 2 ? m.Threshold * PixelsPerInch / 96 : m.Threshold;
            if (value < threshold || !unlocked.Add(m.Id)) continue;
            // Persist aggregate and unlock atomically before announcing it.
            Save(); AchievementUnlocked?.Invoke(new(m.Id,m.Title,m.Description,true));
        }
    }
    private void ObserveInsights(ActivityKind kind,double? observedSeconds=null) { Insights.UpdateContext(day,Today,Lifetime,PixelsPerInch); Insights.Observe(kind,observedSeconds); }
    private void CommitInsights() { if(Insights.RequiresCommit) Save(); }
    public void Save()
    {
        store.Save(day,Today,Lifetime,unlocked,Paused,NotificationsEnabled,Metric,PixelsPerInch,Insights.ExportState());
        Insights.PublishCommittedNotifications();
    }
}





