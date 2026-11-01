// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Globalization;
using System.Text.Json;

namespace TrackMyClicks.Core;

/// <summary>Local aggregate-derived features. No content or key identities enter this component.</summary>
public sealed class ActivityInsights
{
    private readonly Func<DateTime> clock;
    private readonly InsightState state;
    private readonly Func<double> rateClock;
    private readonly Queue<(double At,string Day)> rateSamples=new();
    private readonly Queue<(double At,string Day)> recordRateSamples=new();
    private readonly Dictionary<string,int> recordRateCounts=new();
    private double lastRateTime=double.NegativeInfinity;
    private double lastObservedTime=double.NegativeInfinity;
    private readonly List<PersonalRecord> pendingRecords = new();
    private readonly List<string> pendingAchievements = new();
    private readonly List<string> pendingChallenges = new();
    private string day;
    private Totals today;
    private Totals lifetime;
    private double pixelsPerInch;
    public int CurrentClicksPerMinute { get { RefreshRate(); return rateSamples.Count; } }
    public int HighestClicksPerMinute => (int)state.Records["cpm"].Value;
    public int TodayPeakClicksPerMinute => state.DailyRatePeaks.GetValueOrDefault(day);
    public int CurrentStreak { get; private set; }
    public int LongestStreak { get; private set; }
    public IReadOnlyList<PersonalRecord> Records => state.Records.Values.OrderBy(r => r.Id, StringComparer.Ordinal).ToArray();
    public IReadOnlyList<ExpandedAchievement> ExpandedAchievements => ExpansionRules.Achievements.Select(View).ToArray();
    public int UnlockedAchievementCount => state.Unlocks.Count;
    public int TotalPoints => state.ChallengeRewards.Values.Sum(c=>c.Points);
    public int ChallengeCompletions => state.ChallengeRewards.Count;
    public IReadOnlyList<DailyChallenge> DailyChallenges => state.ChallengeDays[day].Assignments.Select(a=>ChallengeView(day,a)).ToArray();
    public IReadOnlyList<DailyChallenge> CompletedChallenges => state.ChallengeRewards.Values.OrderByDescending(c=>c.CompletedAt).ToArray();
    internal bool RequiresCommit => pendingRecords.Count > 0 || pendingAchievements.Count > 0 || pendingChallenges.Count > 0;
    public event Action<PersonalRecord>? RecordEstablished;
    public event Action<ExpandedAchievement>? ExpandedAchievementUnlocked;
    public event Action<DailyChallenge>? ChallengeCompleted;
    public string GetFunnyFact(int rotationIndex)
    {
        var facts=new List<string>();
        if(lifetime.Clicks>0)facts.Add($"A workout for your counter: {lifetime.Clicks:N0} recorded mouse {(lifetime.Clicks==1?"click":"clicks")}. Every click counts.");
        if(lifetime.KeyPresses>0)facts.Add($"Your keyboard counter has survived {lifetime.KeyPresses:N0} {(lifetime.KeyPresses==1?"press":"presses")}—without storing a single key identity.");
        if(lifetime.DistancePixels>0)
        {
            double meters=lifetime.DistancePixels/pixelsPerInch*.0254;
            facts.Add($"Estimated pointer travel: {meters:N1} meters using your current calibration. Your physical mouse may have a different story.");
            if(meters>=100)facts.Add($"Estimated comparison: your pointer covered about {meters/100:N1} stretches of a 100-meter straight, based on current pointer calibration.");
        }
        if(lifetime.ScrollUnits>0)facts.Add($"Your scroll wheel has logged {lifetime.ScrollUnits:N1} {(lifetime.ScrollUnits==1?"notch":"notches")} of activity. No document contents needed.");
        if(LongestStreak>0)facts.Add($"Your longest recorded streak is {LongestStreak:N0} active local-calendar {(LongestStreak==1?"day":"days")}. Taking a break is always allowed.");
        if(HighestClicksPerMinute>0)facts.Add($"Your busiest observed rolling minute holds {HighestClicksPerMinute:N0} {(HighestClicksPerMinute==1?"click":"clicks")}. That is a real count, not a startup estimate.");
        if(facts.Count==0)return "No activity recorded yet. Use your computer normally; the counter will find its own stories. Pause whenever you like.";
        int index=((rotationIndex%facts.Count)+facts.Count)%facts.Count;
        return facts[index];
    }
    private static string Day(DateTime value) => value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    internal ActivityInsights(Store store, Func<DateTime> clock, string day, Totals today, Totals lifetime, double ppi,Func<double>? rateClock=null)
    {
        this.rateClock=rateClock ?? (()=>(double)System.Diagnostics.Stopwatch.GetTimestamp()/System.Diagnostics.Stopwatch.Frequency);
        this.clock=clock; this.day=day; this.today=today; this.lifetime=lifetime; pixelsPerInch=ppi;
        string? json=store.ReadExpansionState();
        try
        {
            if(json is not null)
            {
                using var document=JsonDocument.Parse(json);
                if(document.RootElement.ValueKind!=JsonValueKind.Object || !document.RootElement.TryGetProperty("Version",out var version) || version.ValueKind!=JsonValueKind.Number || !version.TryGetInt32(out int parsedVersion) || parsedVersion!=1)
                    throw new IOException("Unsupported or incomplete activity expansion state. Data has not been reset.");
            }
            state=json is null ? new() : JsonSerializer.Deserialize<InsightState>(json) ?? throw new IOException("Empty activity expansion state.");
        }
        catch(JsonException ex) { throw new IOException("The local activity expansion state could not be read. Data has not been reset.",ex); }
        if(state.Version!=1) throw new IOException("Unsupported activity expansion state version. Data has not been reset.");
        if(state.ActiveDays is null || state.Records is null || state.RecordNotices is null || state.DailyRatePeaks is null || state.Unlocks is null || state.ChallengeDays is null || state.ChallengeRewards is null)
            throw new IOException("Incomplete activity expansion state. Data has not been reset.");
        foreach(var entry in store.ReadDailyTotals().OrderBy(e=>e.Key,StringComparer.Ordinal))
        {
            if(!DateTime.TryParseExact(entry.Key,"yyyy-MM-dd",CultureInfo.InvariantCulture,DateTimeStyles.None,out var date)) continue;
            if(HasActivity(entry.Value)) state.ActiveDays.Add(entry.Key);
            UpdateDailyRecords(entry.Value,date,true);
        }
        foreach(var definition in RecordDefinitions)
            if(!state.Records.ContainsKey(definition.Id)) state.Records[definition.Id]=new(definition.Id,definition.Title,0,null,definition.Unit);
        UpdateContext(day,today,lifetime,ppi); RecalculateStreaks(true);
        foreach(var id in store.ReadAchievements())
            if(!state.Unlocks.ContainsKey(id)) state.Unlocks[id]=new(){At=null,Imported=true};
        UpdateChallenges(true); EvaluateAchievements(true);
    }
    private static readonly (string Id,string Title,string Unit)[] RecordDefinitions =
    [ ("clicks","Most mouse clicks in a day","clicks"), ("keys","Most keyboard presses in a day","presses"),
      ("movement","Greatest pointer distance in a day","pixels"), ("scroll","Most scrolling in a day","notches"),
      ("cpm","Highest observed clicks per minute","clicks/min"), ("streak","Longest activity streak","days") ];
    private static bool HasActivity(Totals t) => t.Clicks>0 || t.KeyPresses>0 || t.DistancePixels>0 || t.ScrollUnits>0;
    internal void UpdateContext(string day, Totals today, Totals lifetime, double ppi)
    { this.day=day; this.today=today; this.lifetime=lifetime; pixelsPerInch=ppi; EnsureChallengeDay(); Refresh(); }
    public void Refresh()
    {
        var current=clock().Date;
        if(!state.ActiveDays.Contains(Day(current))) current=current.AddDays(-1);
        int streak=0;
        while(state.ActiveDays.Contains(Day(current))) { streak++; current=current.AddDays(-1); }
        CurrentStreak=streak; RefreshRate();
    }
    internal void Observe(ActivityKind kind,double? observedSeconds=null)
    {
        if(state.ActiveDays.Add(day)) RecalculateStreaks(false);
        UpdateDailyRecords(today, DateTime.ParseExact(day,"yyyy-MM-dd",CultureInfo.InvariantCulture), false);
        if(kind==ActivityKind.Click) ObserveClick(observedSeconds ?? rateClock());
        UpdateChallenges(false); EvaluateAchievements(false);
    }
    private void EnsureChallengeDay()
    {
        if(state.ChallengeDays.ContainsKey(day))return;
        var local=DateTime.ParseExact(day,"yyyy-MM-dd",CultureInfo.InvariantCulture);
        state.ChallengeDays[day]=new(){Assignments=ExpansionRules.SelectChallenges(local).Select(d=>new ChallengeProgress{Definition=d}).ToList()};
    }
    private double ChallengeValue(InsightMetric metric) => metric switch
    {
        InsightMetric.Clicks=>today.Clicks,
        InsightMetric.Keyboard=>today.KeyPresses,
        InsightMetric.MovementMeters=>today.DistancePixels/pixelsPerInch*.0254,
        InsightMetric.Scroll=>today.ScrollUnits,
        InsightMetric.ClickRate=>TodayPeakClicksPerMinute,
        _=>0
    };
    private DailyChallenge ChallengeView(string localDay,ChallengeProgress assignment)
    {
        var d=assignment.Definition; string id=localDay+"/"+d.Id;
        state.ChallengeRewards.TryGetValue(id,out var reward);
        return new(id,d.Id,localDay,d.Title,d.Description,d.Metric,assignment.Current,d.Target,reward!=null,d.Points,reward?.CompletedAt);
    }
    private void UpdateChallenges(bool historical)
    {
        EnsureChallengeDay();
        foreach(var assignment in state.ChallengeDays[day].Assignments)
        {
            assignment.Current=Math.Max(assignment.Current,ChallengeValue(assignment.Definition.Metric));
            var view=ChallengeView(day,assignment);
            if(view.Completed || view.Current<view.Target)continue;
            var completed=view with {Completed=true,CompletedAt=clock()};
            state.ChallengeRewards[view.Id]=completed;
            if(!historical)pendingChallenges.Add(view.Id);
        }
    }
    private double AchievementValue(AchievementDefinition definition) => definition.Scope switch
    {
        AchievementScope.Streak => LongestStreak,
        AchievementScope.Challenges => ChallengeCompletions,
        AchievementScope.Daily => definition.Metric switch
        {
            InsightMetric.Clicks => state.Records["clicks"].Value,
            InsightMetric.Keyboard => state.Records["keys"].Value,
            InsightMetric.MovementMeters => state.Records["movement"].Value/pixelsPerInch*.0254,
            InsightMetric.Scroll => state.Records["scroll"].Value,
            _ => 0
        },
        _ => definition.Metric switch
        {
            InsightMetric.Clicks => lifetime.Clicks,
            InsightMetric.Keyboard => lifetime.KeyPresses,
            InsightMetric.MovementMeters => lifetime.DistancePixels/pixelsPerInch*.0254,
            InsightMetric.Scroll => lifetime.ScrollUnits,
            _ => 0
        }
    };
    private ExpandedAchievement View(AchievementDefinition definition)
    {
        state.Unlocks.TryGetValue(definition.Id,out var stamp);
        return new(definition.Id,definition.Category,definition.Tier,definition.Title,definition.Description,AchievementValue(definition),definition.Target,stamp!=null,stamp?.At,stamp?.Imported??false);
    }
    private void EvaluateAchievements(bool historical)
    {
        foreach(var definition in ExpansionRules.Achievements)
        {
            if(state.Unlocks.ContainsKey(definition.Id) || AchievementValue(definition)<definition.Target)continue;
            state.Unlocks[definition.Id]=new(){At=historical?null:clock(),Imported=historical};
            if(!historical)pendingAchievements.Add(definition.Id);
        }
    }
    private void RefreshRate()
    {
        double now=rateClock();
        if(now<lastRateTime) { rateSamples.Clear(); recordRateSamples.Clear(); recordRateCounts.Clear(); lastObservedTime=double.NegativeInfinity; }
        lastRateTime=now;
        while(rateSamples.TryPeek(out var sample) && sample.At<=now-60) rateSamples.Dequeue();
    }
    private void ObserveClick(double observedSeconds)
    {
        RefreshRate(); double now=rateClock();
        if(!double.IsFinite(observedSeconds) || observedSeconds>now || observedSeconds<lastObservedTime) return;
        while(recordRateSamples.TryPeek(out var old) && old.At<=observedSeconds-60)
        {
            recordRateSamples.Dequeue();
            if(--recordRateCounts[old.Day]==0)recordRateCounts.Remove(old.Day);
        }
        recordRateSamples.Enqueue((observedSeconds,day));
        recordRateCounts[day]=recordRateCounts.GetValueOrDefault(day)+1;
        if(observedSeconds>now-60) rateSamples.Enqueue((observedSeconds,day));
        lastObservedTime=observedSeconds;
        SetRecord("cpm",recordRateSamples.Count,DateTime.ParseExact(day,"yyyy-MM-dd",CultureInfo.InvariantCulture),false);
        int todayCount=recordRateCounts[day];
        state.DailyRatePeaks[day]=Math.Max(TodayPeakClicksPerMinute,todayCount);
    }    private void UpdateDailyRecords(Totals t,DateTime date,bool historical)
    {
        SetRecord("clicks",t.Clicks,date,historical); SetRecord("keys",t.KeyPresses,date,historical);
        SetRecord("movement",t.DistancePixels,date,historical); SetRecord("scroll",t.ScrollUnits,date,historical);
    }
    private void SetRecord(string id,double value,DateTime date,bool historical)
    {
        var definition=RecordDefinitions.First(d=>d.Id==id);
        if(state.Records.TryGetValue(id,out var previous) && value<=previous.Value) return;
        if(value<=0) return;
        var record=new PersonalRecord(id,definition.Title,value,date.Date,definition.Unit);
        state.Records[id]=record;
        if(!historical && state.RecordNotices.Add(Day(date)+"/"+id)) pendingRecords.Add(record);
    }
    private void RecalculateStreaks(bool historical)
    {
        var dates=state.ActiveDays.Select(d=>DateTime.ParseExact(d,"yyyy-MM-dd",CultureInfo.InvariantCulture)).Order().ToArray();
        int run=0,best=0; DateTime? previous=null,bestDate=null;
        foreach(var date in dates)
        {
            run=previous.HasValue && date==previous.Value.AddDays(1) ? run+1 : 1;
            if(run>best) { best=run; bestDate=date; } previous=date;
        }
        LongestStreak=best;
        var current=clock().Date;
        if(!state.ActiveDays.Contains(Day(current))) current=current.AddDays(-1);
        int streak=0;
        while(state.ActiveDays.Contains(Day(current))) { streak++; current=current.AddDays(-1); }
        CurrentStreak=streak;
        if(bestDate.HasValue) SetRecord("streak",best,bestDate.Value,historical);
    }
    internal string ExportState() => JsonSerializer.Serialize(state);
    internal void PublishCommittedNotifications()
    {
        var records=pendingRecords.ToArray(); pendingRecords.Clear();
        var achievements=pendingAchievements.ToArray(); pendingAchievements.Clear();
        var challenges=pendingChallenges.ToArray(); pendingChallenges.Clear();
        foreach(var record in records) RecordEstablished?.Invoke(state.Records[record.Id]);
        foreach(var id in achievements) ExpandedAchievementUnlocked?.Invoke(View(ExpansionRules.Achievements.First(a=>a.Id==id)));
        foreach(var id in challenges) ChallengeCompleted?.Invoke(state.ChallengeRewards[id]);
    }
}



