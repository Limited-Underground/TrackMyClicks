// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using TrackMyClicks.Core;
var root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
int checks=0;
void Assert(bool condition,string name){if(!condition)throw new Exception("FAIL: "+name);Console.WriteLine("PASS: "+name);checks++;}
DateTime now=new(2026,10,1,12,0,0); var path=Path.Combine(root,"expansion.db");
using(var store=new Store(path))
{
 var engine=new ActivityEngine(store,()=>now); int notices=0;engine.Insights.RecordEstablished+=_=>notices++;
 engine.Paused=true; engine.Click(MouseButton.Left); engine.Move(2); engine.Scroll(120); engine.KeyPress();
 Assert(engine.Insights.CurrentStreak==0,"stage3 pause does not create active day");
 engine.Paused=false; engine.Move(1); Assert(engine.Insights.CurrentStreak==1,"stage3 pointer input creates active day");
 engine.Click(MouseButton.Left);int firstNotices=notices; for(int i=0;i<20;i++)engine.Click(MouseButton.Left);
 Assert(notices==firstNotices,"stage3 record notices throttled once per category per local day");
 Assert(engine.Insights.Records.First(r=>r.Id=="clicks").Value==21,"stage3 record value continues improving after notice");
 now=now.AddDays(1);engine.RefreshDay();Assert(engine.Insights.CurrentStreak==1,"stage3 yesterday grace before new input");
 engine.Scroll(-120);Assert(engine.Insights.CurrentStreak==2 && engine.Insights.LongestStreak==2,"stage3 consecutive scroll day increments streak");
 now=now.AddDays(2);engine.RefreshDay(); Assert(engine.Insights.CurrentStreak==0 && engine.Insights.LongestStreak==2,"stage3 genuine inactive gap ends current streak only");
 engine.KeyPress();engine.Save();
}
using(var store=new Store(path))
{
 var engine=new ActivityEngine(store,()=>now);Assert(engine.Insights.CurrentStreak==1 && engine.Insights.LongestStreak==2,"stage3 restart preserves current and longest streak");
 Assert(engine.Insights.Records.First(r=>r.Id=="clicks").Value==21 && engine.Insights.Records.First(r=>r.Id=="clicks").Date==new DateTime(2026,10,1),"stage3 daily record and achieved date survive restart");
}
var legacyPath=Path.Combine(root,"legacy.db");
using(var store=new Store(legacyPath))
{
 var total=new Totals(); foreach(var d in new[]{"2026-09-28","2026-09-29","2026-09-30"}) {var daily=new Totals{KeyPresses=5};total.KeyPresses+=5;store.Save(d,daily,total,Array.Empty<string>(),false,true,false);}
 now=new(2026,10,1,12,0,0);var engine=new ActivityEngine(store,()=>now);Assert(engine.Insights.CurrentStreak==3 && engine.Insights.LongestStreak==3,"stage3 imports preexisting daily activity without reset");
 Assert(engine.Insights.Records.First(r=>r.Id=="keys").Date==new DateTime(2026,9,28),"stage3 imported tied record keeps first achieved date");
}
double seconds=0; now=new(2026,10,10,12,0,0);var ratePath=Path.Combine(root,"rate.db");
using(var store=new Store(ratePath))
{
 var engine=new ActivityEngine(store,()=>now,()=>seconds);engine.Click(MouseButton.Left,0);
 Assert(engine.Insights.CurrentClicksPerMinute==1,"stage4 startup rate counts one observed click without extrapolation");
 seconds=59.999;engine.Click(MouseButton.Right,seconds);Assert(engine.Insights.CurrentClicksPerMinute==2,"stage4 rolling window includes clicks younger than sixty seconds");
 seconds=60;Assert(engine.Insights.CurrentClicksPerMinute==1,"stage4 exact sixty-second boundary expires old click");
 engine.Paused=true;engine.Click(MouseButton.Middle,seconds);Assert(engine.Insights.CurrentClicksPerMinute==1 && engine.Lifetime.Clicks==2,"stage4 pause excludes clicks from rate and totals");
 seconds=120;engine.Insights.Refresh();Assert(engine.Insights.CurrentClicksPerMinute==0,"stage4 paused rolling rate decays to zero");
 engine.Paused=false;engine.Click(MouseButton.Left,130);Assert(engine.Insights.CurrentClicksPerMinute==0,"stage4 future timestamp cannot inflate live rate");
 engine.Save();
}
using(var store=new Store(ratePath))
{
 var engine=new ActivityEngine(store,()=>now,()=>seconds);Assert(engine.Insights.CurrentClicksPerMinute==0 && engine.Insights.HighestClicksPerMinute==2,"stage4 restart clears transient window and preserves actual personal best");
}
using(var store=new Store(Path.Combine(root,"backlog.db")))
{
 seconds=70;var engine=new ActivityEngine(store,()=>now,()=>seconds);engine.Click(MouseButton.Left,0);engine.Click(MouseButton.Left,1);
 Assert(engine.Insights.CurrentClicksPerMinute==0 && engine.Insights.HighestClicksPerMinute==2,"stage4 delayed event replay preserves historical best but not stale live rate");
}
using(var store=new Store(Path.Combine(root,"midnight-rate.db")))
{
 seconds=0;now=new(2026,10,10,23,59,59);var engine=new ActivityEngine(store,()=>now,()=>seconds);engine.Click(MouseButton.Left,0);
 seconds=1;now=now.AddSeconds(2);engine.Click(MouseButton.Left,1);
 Assert(engine.Insights.HighestClicksPerMinute==2 && engine.Insights.TodayPeakClicksPerMinute==1,"stage4 midnight global window crosses days but daily peak excludes yesterday");
}
Assert(ExpansionRules.Achievements.Select(a=>a.Id).Distinct().Count()==ExpansionRules.Achievements.Count,"stage5 central achievement catalog has unique stable identities");
Assert(ExpansionRules.Achievements.Where(a=>a.Scope==AchievementScope.Lifetime).GroupBy(a=>a.Category).All(g=>g.Select(a=>a.Tier).Distinct().Count()==5),"stage5 lifetime categories include all five visual tiers");
Assert(ExpansionRules.Achievements.Where(a=>a.Scope==AchievementScope.Streak).Select(a=>a.Target).SequenceEqual(new double[]{3,7,14,30,60,100}),"stage5 required streak milestone thresholds are exact");
var assignment=ExpansionRules.SelectChallenges(new DateTime(2026,10,29));
Assert(assignment.Count==3 && assignment.Select(c=>c.Metric).Distinct().Count()==3 && assignment.Select(c=>c.Id).SequenceEqual(ExpansionRules.SelectChallenges(new DateTime(2026,10,29)).Select(c=>c.Id)),"stage5 daily challenge rules choose three deterministic varied categories");
using(var store=new Store(Path.Combine(root,"tiers.db")))
{
 now=new(2026,10,1);seconds=0;var engine=new ActivityEngine(store,()=>now,()=>seconds);int unlocks=0;engine.Insights.ExpandedAchievementUnlocked+=_=>unlocks++;
 for(int i=0;i<100;i++)engine.Click(MouseButton.Left,seconds);
 Assert(engine.Achievements.Count==9 && engine.Achievements.First(a=>a.Id=="click100").Unlocked,"stage5 original nine achievement API and legacy threshold preserved");
 var bronze=engine.Insights.ExpandedAchievements.First(a=>a.Id=="click100");
 Assert(bronze.Unlocked && bronze.Tier==AchievementTier.Bronze && bronze.UnlockedAt==now && !bronze.LegacyUnknownDate,"stage5 new tier unlock records actual local unlock date");
 int earned=unlocks;engine.Click(MouseButton.Left,seconds);Assert(unlocks==earned,"stage5 earned tiers never unlock twice on subsequent input");
 engine.Save();
}
using(var store=new Store(Path.Combine(root,"tiers.db")))
{
 var engine=new ActivityEngine(store,()=>now,()=>seconds);Assert(engine.Insights.ExpandedAchievements.First(a=>a.Id=="click100").UnlockedAt==now,"stage5 tier date survives restart");
}
using(var store=new Store(Path.Combine(root,"old-badge.db")))
{
 store.Save("2026-10-01",new(),new(),new[]{"move100"},false,true,false,192);var engine=new ActivityEngine(store,()=>now,()=>seconds);
 var badge=engine.Insights.ExpandedAchievements.First(a=>a.Id=="move100");Assert(badge.Unlocked && badge.UnlockedAt==null && badge.LegacyUnknownDate,"stage5 imported badge remains earned with honest unknown date even below calibrated threshold");
}
var challengePath=Path.Combine(root,"challenge.db");
var challengeDay=Enumerable.Range(0,12).Select(i=>new DateTime(2026,11,1).AddDays(i)).First(d=>ExpansionRules.SelectChallenges(d).Any(c=>c.Metric==InsightMetric.Clicks));
string[] challengeIds;now=challengeDay;seconds=0;
using(var store=new Store(challengePath))
{
 var engine=new ActivityEngine(store,()=>now,()=>seconds);challengeIds=engine.Insights.DailyChallenges.Select(c=>c.Id).ToArray();
 for(int i=0;i<100;i++)engine.Click(MouseButton.Left,seconds);engine.Save();
 Assert(engine.Insights.DailyChallenges.First(c=>c.Metric==InsightMetric.Clicks).Current==100,"stage6 challenge tracks real partial progress");
 engine.Paused=true;for(int i=0;i<1000;i++)engine.Click(MouseButton.Left,seconds);
 Assert(engine.Insights.DailyChallenges.First(c=>c.Metric==InsightMetric.Clicks).Current==100 && engine.Insights.TotalPoints==0,"stage6 paused inputs never advance or reward challenges");
 engine.Paused=false;engine.Save();
}
using(var store=new Store(challengePath))
{
 var engine=new ActivityEngine(store,()=>now,()=>seconds);Assert(engine.Insights.DailyChallenges.Select(c=>c.Id).SequenceEqual(challengeIds) && engine.Insights.DailyChallenges.First(c=>c.Metric==InsightMetric.Clicks).Current==100,"stage6 same-day assignments and progress survive restart");
 int completed=0;engine.Insights.ChallengeCompleted+=_=>completed++;
 for(int i=0;i<900;i++)engine.Click(MouseButton.Left,seconds);
 Assert(completed==1 && engine.Insights.TotalPoints==25 && engine.Insights.ChallengeCompletions==1,"stage6 target awards one dated reward and points");
 for(int i=0;i<100;i++)engine.Click(MouseButton.Left,seconds);engine.Save();Assert(completed==1 && engine.Insights.TotalPoints==25,"stage6 subsequent qualifying inputs cannot duplicate reward");
 Assert(engine.Insights.ExpandedAchievements.First(a=>a.Id=="challenges1").Unlocked,"stage6 challenge completion also unlocks configured completion badge");
}
using(var store=new Store(challengePath))
{
 var engine=new ActivityEngine(store,()=>now,()=>seconds);int completed=0;engine.Insights.ChallengeCompleted+=_=>completed++;engine.Click(MouseButton.Left,seconds);
 Assert(engine.Insights.TotalPoints==25 && completed==0 && engine.Insights.CompletedChallenges.Single().CompletedAt==challengeDay,"stage6 reward history date and duplicate prevention survive restart");
 now=now.AddDays(1);engine.RefreshInsights();
 Assert(engine.Insights.DailyChallenges.Count==3 && !engine.Insights.DailyChallenges.Any(c=>challengeIds.Contains(c.Id)) && engine.Insights.DailyChallenges.All(c=>c.Current==0),"stage6 midnight creates dated assignments with zero progress");
 Assert(engine.Insights.CompletedChallenges.Count==1 && engine.Insights.TotalPoints==25,"stage6 new day keeps historical challenge badges and points");
}
using(var store=new Store(Path.Combine(root,"facts.db")))
{
 now=new(2026,12,1);seconds=0;var engine=new ActivityEngine(store,()=>now,()=>seconds);
 Assert(engine.Insights.GetFunnyFact(0).StartsWith("No activity recorded yet"),"stage7 new-user fact is sensible and invents no activity");
 engine.Click((MouseButton)999);engine.Scroll(0);engine.Move(double.NaN);
 Assert(engine.Insights.CurrentStreak==0,"stage7 invalid and zero input cannot create activity streak");
 engine.Click(MouseButton.Left,seconds);engine.KeyPress();engine.Move(100*96/.0254);engine.Scroll(240);
 var facts=Enumerable.Range(0,12).Select(engine.Insights.GetFunnyFact).Distinct().ToArray();
 Assert(facts.Any(f=>f.Contains("1 recorded mouse click.")) && facts.Any(f=>f.Contains("1 press—")) && facts.Any(f=>f.Contains("1 active local-calendar day.")) && facts.Any(f=>f.Contains("2.0 notches")),"stage7 facts use actual totals and correct singular/plural grammar");
 Assert(facts.Any(f=>f.Contains("Estimated comparison") && f.Contains("1.0 stretches of a 100-meter straight")),"stage7 movement comparison states estimate and explicit real-world basis");
 Assert(engine.Insights.GetFunnyFact(int.MinValue).Length>0 && engine.Insights.GetFunnyFact(-1).Length>0,"stage7 rotation handles negative and extreme index safely");
 engine.PixelsPerInch=192;engine.RefreshInsights();
 Assert(Enumerable.Range(0,12).Select(engine.Insights.GetFunnyFact).Any(f=>f.Contains("50.0 meters")),"stage7 estimated facts reflect current configured calibration");
}
foreach(var invalidState in new[]{"{}","{\"Version\":1,\"ChallengeRewards\":null}"})
{
 using var store=new Store(Path.Combine(root,"invalid-state-"+Guid.NewGuid().ToString("N")+".db"));
 store.Save("2026-12-01",new(),new(),Array.Empty<string>(),false,true,false,96,invalidState);
 bool rejected=false;try{_ = new ActivityEngine(store,()=>now,()=>seconds);}catch(IOException){rejected=true;}
 Assert(rejected && store.ReadExpansionState()==invalidState,"stage7 incomplete expansion state is rejected without erasing evidence");
}
Console.WriteLine($"{checks} expansion checks passed. Evidence: {root}");
