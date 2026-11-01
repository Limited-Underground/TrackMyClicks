// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Globalization;
namespace TrackMyClicks.Core;

public enum AchievementTier { Bronze, Silver, Gold, Platinum, Diamond }
public enum AchievementScope { Lifetime, Daily, Streak, Challenges }
public enum InsightMetric { Clicks, Keyboard, MovementMeters, Scroll, ClickRate, Streak, ChallengeCompletions }
public sealed record AchievementDefinition(string Id,string Category,AchievementTier Tier,string Title,string Description,AchievementScope Scope,InsightMetric Metric,double Target);
public sealed record ChallengeDefinition(string Id,string Title,string Description,InsightMetric Metric,double Target,int Points);

/// <summary>One configurable catalog shared by the domain and presentation; existing V0.2 IDs are immutable.</summary>
public static class ExpansionRules
{
    public static IReadOnlyList<AchievementDefinition> Achievements { get; } = BuildAchievements();
    public static IReadOnlyList<ChallengeDefinition> ChallengePool { get; } = new ChallengeDefinition[]
    {
        new("click1000","Click collector","Record 1,000 mouse clicks today, at your own pace.",InsightMetric.Clicks,1000,25),
        new("key5000","Keyboard rhythm","Record 5,000 keyboard presses during your normal day.",InsightMetric.Keyboard,5000,25),
        new("scroll500","Scroll explorer","Record 500 scroll notches today.",InsightMetric.Scroll,500,25),
        new("movehalfmi","Pointer wanderer","Record an estimated 0.5 miles of pointer travel today.",InsightMetric.MovementMeters,804.672,25),
        new("speed100","A busy minute","Observe 100 clicks in a rolling minute, only if your normal activity gets there.",InsightMetric.ClickRate,100,25)
    };
    public static IReadOnlyList<ChallengeDefinition> SelectChallenges(DateTime localDay)
    {
        int ordinal=DateOnly.FromDateTime(localDay).DayNumber;
        int start=ordinal%4;
        // Speed is an alternate mouse challenge, never a second mouse assignment on the same day.
        return Enumerable.Range(0,3).Select(offset=>(start+offset)%4)
            .Select(index=>ChallengePool[index==0 && ordinal%2==1 ? 4 : index]).ToArray();
    }
    private static IReadOnlyList<AchievementDefinition> BuildAchievements()
    {
        var list=new List<AchievementDefinition>();
        void Add(string category,AchievementScope scope,InsightMetric metric,double[] targets,string unit,string prefix)
        {
            for(int i=0;i<targets.Length;i++)
            {
                double target=targets[i]; var tier=(AchievementTier)Math.Min(i,4);
                string id=prefix+target.ToString("0",CultureInfo.InvariantCulture);
                string title=$"{category} · {tier}";
                if(id=="click100")title="First century"; if(id=="click1000")title="Click collector"; if(id=="click10000")title="Click champion";
                if(id=="key1000")title="Keyboard warm-up"; if(id=="key10000")title="Key explorer"; if(id=="key100000")title="Keyboard marathon";
                if(id=="move100")title="On the move"; if(id=="move1000")title="Pointer kilometer"; if(id=="move10000")title="Long-distance pointer";
                string period=scope==AchievementScope.Daily?" in one local day":scope==AchievementScope.Lifetime?" lifetime":"";
                list.Add(new(id,category,tier,title,$"{target:N0} {unit}{period}",scope,metric,target));
            }
        }
        Add("Lifetime clicks",AchievementScope.Lifetime,InsightMetric.Clicks,[100,1000,10000,100000,1000000],"mouse clicks","click");
        Add("Lifetime keyboard",AchievementScope.Lifetime,InsightMetric.Keyboard,[1000,10000,100000,1000000,10000000],"keyboard presses","key");
        Add("Lifetime movement",AchievementScope.Lifetime,InsightMetric.MovementMeters,[100,1000,10000,100000,1000000],"estimated meters","move");
        Add("Lifetime scrolling",AchievementScope.Lifetime,InsightMetric.Scroll,[1000,5000,10000,50000,100000],"scroll notches","scroll");
        Add("Daily clicks",AchievementScope.Daily,InsightMetric.Clicks,[100,500,1000,2500,5000],"mouse clicks","dailyclick");
        Add("Daily keyboard",AchievementScope.Daily,InsightMetric.Keyboard,[500,1000,2500,5000,10000],"keyboard presses","dailykey");
        Add("Daily movement",AchievementScope.Daily,InsightMetric.MovementMeters,[25,100,250,500,1000],"estimated meters","dailymove");
        Add("Daily scrolling",AchievementScope.Daily,InsightMetric.Scroll,[50,100,250,500,1000],"scroll notches","dailyscroll");
        Add("Activity streak",AchievementScope.Streak,InsightMetric.Streak,[3,7,14,30,60,100],"consecutive active days","streak");
        Add("Challenge completions",AchievementScope.Challenges,InsightMetric.ChallengeCompletions,[1,3,10,30,100],"daily challenges completed","challenges");
        return list.AsReadOnly();
    }
}
