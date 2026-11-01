// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using TrackMyClicks.Core;
var root=Path.Combine(AppContext.BaseDirectory,"test-data",Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var path=Path.Combine(root,"activity.db");
int checks=0;
void Assert(bool value,string name) { if(!value) throw new Exception("FAIL: "+name); checks++; Console.WriteLine("PASS: "+name); }
DateTime now=new(2026,10,29,23,59,58);
using(var store=new Store(path))
{
    var engine=new ActivityEngine(store,()=>now);
    engine.Click(MouseButton.Left); engine.Click(MouseButton.Right); engine.Click(MouseButton.Middle); engine.KeyPress(); engine.Move(123.5); engine.Scroll(120); engine.Scroll(-60);
    Assert(engine.Today.Clicks==3 && engine.Today.LeftClicks==1 && engine.Today.RightClicks==1 && engine.Today.MiddleClicks==1,"mouse button counts");
    Assert(engine.Today.KeyPresses==1 && engine.Today.DistancePixels==123.5 && engine.Today.ScrollUnits==1.5,"keyboard, movement, bidirectional scroll magnitude");
    engine.Move(double.NaN); engine.Move(double.PositiveInfinity); engine.Move(-2);
    Assert(engine.Today.DistancePixels==123.5,"invalid movement ignored");
    engine.Paused=true; engine.Click(MouseButton.Left); engine.KeyPress(); engine.Move(55); engine.Scroll(120);
    Assert(engine.Today.Clicks==3 && engine.Today.KeyPresses==1 && engine.Today.DistancePixels==123.5 && engine.Today.ScrollUnits==1.5,"pause suppresses all inputs");
    engine.Save(); now=now.AddSeconds(5); engine.RefreshDay();
    Assert(engine.Today.Clicks==0 && engine.Lifetime.Clicks==3 && engine.Paused,"daily rollover while paused preserves lifetime");
    engine.Paused=false; int unlocks=0; engine.AchievementUnlocked+=_=>unlocks++;
    for(int i=0;i<98;i++) engine.Click(MouseButton.Left);
    Assert(unlocks==1 && engine.Achievements.Count(a=>a.Unlocked)==1,"click milestone unlocks exactly once across threshold");
    for(int i=0;i<999;i++) engine.KeyPress();
    engine.Move(100 * 96 / .0254);
    Assert(unlocks==3,"keyboard and movement milestones");
    engine.NotificationsEnabled=false; engine.Metric=true; engine.PixelsPerInch=110; engine.Save();
}
using(var store=new Store(path))
{
    var engine=new ActivityEngine(store,()=>now);
    Assert(engine.Today.Clicks==98 && engine.Lifetime.Clicks==101 && engine.Lifetime.KeyPresses==1000,"restart restores daily and lifetime totals");
    Assert(engine.Achievements.Count(a=>a.Unlocked)==3,"restart restores achievement unlocks");
    int unlocks=0; engine.AchievementUnlocked+=_=>unlocks++; engine.Click(MouseButton.Left); engine.KeyPress(); engine.Move(1);
    Assert(unlocks==0,"restart never repeats unlocked milestone");
    Assert(!engine.NotificationsEnabled && engine.Metric && engine.PixelsPerInch==110,"settings persist");
    Assert(store.ReadTotals("2026-10-29").Clicks==3,"prior daily history preserved");
    engine.Paused=true; engine.Save();
}
using(var store=new Store(path)) Assert(new ActivityEngine(store,()=>now).Paused,"restart retains visible pause state");
using(var calibrationStore=new Store(Path.Combine(root,"calibration.db")))
{
    var calibrated=new ActivityEngine(calibrationStore,()=>now) { PixelsPerInch=192 };
    calibrated.Move(100 * 96 / .0254);
    Assert(!calibrated.Achievements.First(a=>a.Id=="move100" ).Unlocked,"movement achievement respects configured PPI below threshold");
    calibrated.Move(100 * 96 / .0254);
    Assert(calibrated.Achievements.First(a=>a.Id=="move100" ).Unlocked,"movement achievement respects configured PPI at threshold");
}
Console.WriteLine($"{checks} focused checks passed. SQLite evidence: {path}");

