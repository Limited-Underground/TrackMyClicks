// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
namespace TrackMyClicks.Core;

public sealed record PersonalRecord(string Id, string Title, double Value, DateTime? Date, string Unit);
public enum ActivityKind { Click, Keyboard, Movement, Scroll }

internal sealed class InsightState
{
    public Dictionary<string,DayChallengeState> ChallengeDays { get; set; } = new();
    public Dictionary<string,DailyChallenge> ChallengeRewards { get; set; } = new();
    public Dictionary<string,UnlockStamp> Unlocks { get; set; } = new();
    public int Version { get; set; } = 1;
    public HashSet<string> ActiveDays { get; set; } = new();
    public Dictionary<string, PersonalRecord> Records { get; set; } = new();
    public Dictionary<string,int> DailyRatePeaks { get; set; } = new();
    public HashSet<string> RecordNotices { get; set; } = new();
}

internal sealed class DayChallengeState
{
    public List<ChallengeProgress> Assignments { get; set; } = new();
}
internal sealed class ChallengeProgress
{
    public ChallengeDefinition Definition { get; set; } = null!;
    public double Current { get; set; }
}


public sealed record ExpandedAchievement(string Id,string Category,AchievementTier Tier,string Title,string Description,double Current,double Target,bool Unlocked,DateTime? UnlockedAt,bool LegacyUnknownDate)
{
    public double ProgressPercent => Unlocked?100:Math.Clamp(Current/Target*100,0,100);
}
public sealed record DailyChallenge(string Id,string RuleId,string LocalDay,string Title,string Description,InsightMetric Metric,double Current,double Target,bool Completed,int Points,DateTime? CompletedAt)
{
    public double ProgressPercent => Completed?100:Math.Clamp(Current/Target*100,0,100);
}
internal sealed class UnlockStamp
{
    public DateTime? At { get; set; }
    public bool Imported { get; set; }
}

