// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using TrackMyClicks.Core;
using Controls = System.Windows.Controls;
using Media = System.Windows.Media;
using Forms = System.Windows.Forms;

namespace TrackMyClicks;

public partial class MainWindow : Window
{
    private Store? store;
    private ActivityEngine engine = null!;
    private InputHooks? hooks;
    private Forms.NotifyIcon? tray;
    private readonly DispatcherTimer timer = new() { Interval=TimeSpan.FromMilliseconds(500) };
    // Observation timestamps stay transient. Hook callbacks do no SQLite or UI work.
    private sealed record ObservedClick(Core.MouseButton Button,DateTime LocalAt,double Seconds);
    private sealed record BufferedInput(DateTime At,List<ObservedClick> Clicks,long Keys,double Distance,int Scroll);
    private List<ObservedClick> observedClicks=new();
    private readonly Queue<BufferedInput> pending=new();
    private long keys;
    private double distance;
    private int scroll;
    private DateTime bufferDay=DateTime.Now,trackingTime=DateTime.Now;
    private int ticks;
    private bool ready,closing,trackingFailed;
    private Mutex? instance;
    private string lastRender="";
    private readonly List<AchievementCard> achievementCards=new();
    private readonly Dictionary<string,ExpansionCard> badgeViews=new(StringComparer.Ordinal);
    private readonly Dictionary<string,ExpansionCard> challengeViews=new(StringComparer.Ordinal);
    private readonly ObservableCollection<ExpansionCard> dailyChallengeViews=new();
    private readonly ObservableCollection<ExpansionCard> showcaseChallengeViews=new();
    private readonly List<RecordCard> recordViews=new();
    private AchievementShowcase? showcase;
    private readonly List<string> notices=new();
    private int factIndex;
    private double lastFactAt=double.NegativeInfinity;
    private static double MonotonicNow()=>(double)Stopwatch.GetTimestamp()/Stopwatch.Frequency;

    public MainWindow()
    {
        InitializeComponent();
        try
        {
            instance=new Mutex(true,"Local\\LimitedUnderground.TrackMyClicks.V01",out bool first);
            if(!first) { System.Windows.MessageBox.Show("TrackMyClicks is already running. Open it from the tray."); Close(); return; }
            string path=Environment.GetEnvironmentVariable("TRACKMYCLICKS_DATA_DIR")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Limited Underground","TrackMyClicks");
            Directory.CreateDirectory(path);
            store=new Store(Path.Combine(path,"activity.sqlite3"));
            engine=new ActivityEngine(store,()=>trackingTime);
            achievementCards.AddRange(engine.Achievements.Select(a=>new AchievementCard(a)));
            AchievementCards.ItemsSource=achievementCards.Take(3).ToArray();
            DailyChallengeCards.ItemsSource=dailyChallengeViews;
            foreach(string id in new[]{"clicks","keys","movement","scroll","cpm","streak"})
            {
                var record=engine.Insights.Records.Single(r=>r.Id==id);
                recordViews.Add(new RecordCard(id,record.Title));
            }
            PersonalRecordCards.ItemsSource=recordViews;
            Notifications.IsChecked=engine.NotificationsEnabled;
            Metric.IsChecked=engine.Metric;
            Calibration.Text=engine.PixelsPerInch.ToString(CultureInfo.CurrentCulture);
            tray=new Forms.NotifyIcon { Icon=System.Drawing.SystemIcons.Application,Text="TrackMyClicks",Visible=true };
            var menu=new Forms.ContextMenuStrip();
            menu.Items.Add("Open dashboard",null,(_,_)=>Restore());
            menu.Items.Add("Pause / resume",null,(_,_)=>TogglePause());
            menu.Items.Add("Exit",null,(_,_)=>Close());
            tray.ContextMenuStrip=menu;
            tray.DoubleClick+=(_,_)=>Restore();
            // All events below follow a durable aggregate+reward commit. Batch notices per UI tick.
            engine.Insights.RecordEstablished+=r=>QueueNotice("New record: "+r.Title+" · "+RecordValue(r));
            engine.Insights.ExpandedAchievementUnlocked+=a=>QueueNotice(a.Title+" · "+a.Tier);
            engine.Insights.ChallengeCompleted+=c=>QueueNotice(c.Title+" completed · +"+c.Points+" points");
            hooks=new InputHooks();
            hooks.Click+=i=>
            {
                DateTime observedAt=PrepareInput();
                if(!engine.Paused) observedClicks.Add(new((Core.MouseButton)i,observedAt,MonotonicNow()));
            };
            hooks.Key+=()=> { PrepareInput(); if(!engine.Paused) keys++; };
            hooks.Move+=d=> { PrepareInput(); if(!engine.Paused) distance+=d; };
            hooks.Scroll+=d=> { PrepareInput(); if(!engine.Paused) scroll+=Math.Abs(d); };
            ready=true;
            timer.Tick+=(_,_)=>Tick();
            timer.Start();
            Render();
            SizeChanged+=(_,_)=>UpdateResponsiveLayout();
            UpdateResponsiveLayout();
            StateChanged+=(_,_)=> { if(WindowState==WindowState.Minimized) { showcase?.Close(); Hide(); } };
            SaveText.Text="Saved locally every 5 seconds · "+path;
        }
        catch(Exception ex)
        {
            System.Windows.MessageBox.Show("TrackMyClicks could not start safely.\n"+ex.Message,"Startup failed");
            Close();
        }
    }

    private BufferedInput TakeBuffer()
    {
        var batch=new BufferedInput(bufferDay,observedClicks,keys,distance,scroll);
        observedClicks=new(); keys=0; distance=0; scroll=0;
        return batch;
    }
    private DateTime PrepareInput()
    {
        DateTime now=DateTime.Now;
        if(now.Date!=bufferDay.Date)
        {
            pending.Enqueue(TakeBuffer());
            bufferDay=now;
        }
        return now;
    }
    private void Apply(BufferedInput batch)
    {
        foreach(var click in batch.Clicks)
        {
            trackingTime=click.LocalAt;
            engine.Click(click.Button,click.Seconds);
        }
        trackingTime=batch.At;
        engine.RefreshDay();
        for(long n=0;n<batch.Keys;n++) engine.KeyPress();
        engine.Move(batch.Distance);
        engine.Scroll(batch.Scroll);
    }
    private void Drain()
    {
        // Detach before applying so a storage failure cannot replay a partially applied batch.
        while(pending.TryDequeue(out var batch)) Apply(batch);
        Apply(TakeBuffer());
        trackingTime=DateTime.Now;
        bufferDay=trackingTime;
        engine.RefreshInsights();
    }
    private void Tick()
    {
        try
        {
            Drain();
            if(++ticks%10==0) engine.Save();
            Render();
            FlushNotices();
        }
        catch(Exception ex)
        {
            trackingFailed=true;
            engine.Paused=true;
            timer.Stop();
            hooks?.Dispose();
            SaveText.Text="Tracking stopped: "+ex.Message;
            PauseButton.IsEnabled=false;
            Render();
        }
    }

    private string Distance(double pixels)=>Meters(pixels/engine.PixelsPerInch*.0254);
    private string Meters(double meters)=>engine.Metric
        ? (meters>=1000?$"{meters/1000:N2} km":$"{meters:N1} m")
        : (meters>=1609.344?$"{meters/1609.344:N2} mi":$"{meters/.3048:N1} ft");
    private string RecordValue(PersonalRecord record)=>record.Unit switch
    {
        "pixels"=>Distance(record.Value),
        "notches"=>$"{record.Value:N1} notches",
        "clicks/min"=>$"{record.Value:N0} clicks / 60s",
        "days"=>$"{record.Value:N0} "+(record.Value==1?"day":"days"),
        "clicks"=>$"{record.Value:N0} "+(record.Value==1?"click":"clicks"),
        "presses"=>$"{record.Value:N0} "+(record.Value==1?"press":"presses"),
        _=>$"{record.Value:N0} {record.Unit}"
    };
    private string ProgressText(InsightMetric metric,double current,double target,bool completed)
    {
        double value=completed?target:Math.Min(current,target);
        return metric==InsightMetric.MovementMeters?$"{Meters(value)} / {Meters(target)}"
            : metric==InsightMetric.Scroll?$"{value:N1} / {target:N0}"
            : $"{Math.Floor(value):N0} / {target:N0}";
    }
    private string Summary()
    {
        var i=engine.Insights;
        string challenges=i.ChallengeCompletions==1?"daily challenge completed":"daily challenges completed";
        return $"{i.ExpandedAchievements.Count(a=>a.Unlocked):N0} / {ExpansionRules.Achievements.Count:N0} lifetime badges earned  ·  {i.TotalPoints:N0} points  ·  {i.ChallengeCompletions:N0} {challenges}";
    }
    private static void SyncCards(ObservableCollection<ExpansionCard> collection,IReadOnlyList<ExpansionCard> desired)
    {
        if(collection.Count==desired.Count && collection.Select(c=>c.Id).SequenceEqual(desired.Select(c=>c.Id)))return;
        collection.Clear();
        foreach(var item in desired) collection.Add(item);
    }
    private ExpansionCard ChallengeView(DailyChallenge challenge)
    {
        if(!challengeViews.TryGetValue(challenge.Id,out var card))
            challengeViews[challenge.Id]=card=new ExpansionCard(challenge.Id);
        card.Apply(challenge.Title,challenge.Description,"Daily challenges","Daily",
            ProgressText(challenge.Metric,challenge.Current,challenge.Target,challenge.Completed),
            challenge.ProgressPercent,challenge.Completed,challenge.Completed?"Completed":"In progress",
            challenge.Completed
                ?$"+{challenge.Points:N0} points · {challenge.CompletedAt:MMM d, yyyy}"
                :$"{challenge.LocalDay} · {challenge.Points:N0} points · optional",
            "#27E6AF");
        return card;
    }
    private void RenderExpansion()
    {
        var insights=engine.Insights;
        var expanded=insights.ExpandedAchievements;
        var nextTierIds=expanded.Where(a=>!a.Unlocked).GroupBy(a=>a.Category)
            .Select(g=>g.First().Id).ToHashSet(StringComparer.Ordinal);
        foreach(var achievement in expanded)
        {
            if(!badgeViews.TryGetValue(achievement.Id,out var card))
                badgeViews[achievement.Id]=card=new ExpansionCard(achievement.Id);
            var rule=ExpansionRules.Achievements.Single(r=>r.Id==achievement.Id);
            string date=achievement.Unlocked
                ? achievement.UnlockedAt.HasValue?$"Unlocked {achievement.UnlockedAt:MMM d, yyyy}"
                    :"Imported activity · original unlock date unavailable"
                :"Not earned yet";
            card.Apply(achievement.Title,achievement.Description,achievement.Category,achievement.Tier.ToString(),
                ProgressText(rule.Metric,achievement.Current,achievement.Target,achievement.Unlocked),
                achievement.ProgressPercent,achievement.Unlocked,achievement.Unlocked?"Unlocked":nextTierIds.Contains(achievement.Id)?"Next tier":"Locked",
                date,ExpansionPalette.Hex(achievement.Tier.ToString()));
        }
        foreach(var preview in achievementCards)
            preview.Update(expanded.Single(a=>a.Id==preview.Id));
        var current=insights.DailyChallenges.Select(ChallengeView).ToArray();
        SyncCards(dailyChallengeViews,current);
        var history=insights.DailyChallenges.Concat(insights.CompletedChallenges)
            .DistinctBy(c=>c.Id).Select(ChallengeView).ToArray();
        SyncCards(showcaseChallengeViews,history);
        foreach(var view in recordViews)
        {
            var record=insights.Records.Single(r=>r.Id==view.Id);
            view.Apply(RecordValue(record),record.Date.HasValue?$"Set {record.Date:MMM d, yyyy}":"No record yet");
        }
        StreakCurrentText.Text=$"{insights.CurrentStreak:N0} "+(insights.CurrentStreak==1?"day":"days");
        StreakLongestText.Text=$"{insights.LongestStreak:N0} "+(insights.LongestStreak==1?"day":"days");
        ClickRateText.Text=$"{insights.CurrentClicksPerMinute:N0}";
        ClickRateBestText.Text=$"{insights.HighestClicksPerMinute:N0}";
        RewardPointsText.Text=$"{insights.TotalPoints:N0}";
        double now=MonotonicNow();
        if(now-lastFactAt>=30)
        {
            FunnyFactText.Text=insights.GetFunnyFact(factIndex++);
            lastFactAt=now;
        }
        showcase?.UpdateSummary(Summary());
    }
    private void Render()
    {
        if(!ready)return;
        if(!trackingFailed)engine.RefreshInsights();
        RenderExpansion();
        var today=engine.Today; var lifetime=engine.Lifetime;
        string signature=$"{today.LeftClicks}|{today.RightClicks}|{today.MiddleClicks}|{today.KeyPresses}|{Distance(today.DistancePixels)}|{today.ScrollUnits:N1}|{lifetime.LeftClicks}|{lifetime.RightClicks}|{lifetime.MiddleClicks}|{lifetime.KeyPresses}|{Distance(lifetime.DistancePixels)}|{lifetime.ScrollUnits:N1}|{engine.Paused}|{engine.Metric}|{DateTime.Today:O}";
        if(signature==lastRender)return;
        lastRender=signature;
        LeftToday.Text=$"{today.LeftClicks:N0}"; LeftLifetime.Text=$"{lifetime.LeftClicks:N0}";
        RightToday.Text=$"{today.RightClicks:N0}"; RightLifetime.Text=$"{lifetime.RightClicks:N0}";
        MiddleToday.Text=$"{today.MiddleClicks:N0}"; MiddleLifetime.Text=$"{lifetime.MiddleClicks:N0}";
        KeysToday.Text=$"{today.KeyPresses:N0}"; KeysLifetime.Text=$"{lifetime.KeyPresses:N0}";
        DistanceToday.Text=Distance(today.DistancePixels); DistanceLifetime.Text=Distance(lifetime.DistancePixels);
        ScrollToday.Text=$"{today.ScrollUnits:N1}"; ScrollLifetime.Text=$"{lifetime.ScrollUnits:N1}";
        DateText.Text=DateTime.Now.ToString("dddd, MMMM d, yyyy");
        StatusText.Text=engine.Paused?"Paused":"Tracking locally";
        PauseLabel.Text=engine.Paused?"Resume tracking":"Pause tracking";
        PauseIcon.Data=(Media.Geometry)FindResource(engine.Paused?"PlayGeometry":"PauseGeometry");
        System.Windows.Automation.AutomationProperties.SetName(PauseButton,PauseLabel.Text);
        if(tray!=null)tray.Text=engine.Paused?"TrackMyClicks · Paused":"TrackMyClicks · Tracking";
    }
    private void ShowAllClick(object sender,RoutedEventArgs e)
    {
        Render();
        if(showcase is not null) { showcase.Show(); showcase.Activate(); return; }
        showcase=new AchievementShowcase(badgeViews.Values,showcaseChallengeViews,Summary()) { Owner=this };
        showcase.Closed+=(_,_)=>showcase=null;
        showcase.Show();
    }
    private void UpdateResponsiveLayout()
    {
        bool stacked=ActualWidth<1150;
        DashboardLeftColumn.Width=stacked?new GridLength(1,GridUnitType.Star):new GridLength(1.46,GridUnitType.Star);
        DashboardRightColumn.Width=stacked?new GridLength(0):new GridLength(1,GridUnitType.Star);
        MainDashboard.ColumnDefinitions[1].Width=new GridLength(stacked?0:24);
        Controls.Grid.SetColumn(RightStack,stacked?0:2);
        Controls.Grid.SetRow(RightStack,stacked?1:0);
        RightStack.Margin=stacked?new Thickness(0,20,0,0):new Thickness(0);
    }
    private void QueueNotice(string message) { if(engine.NotificationsEnabled)notices.Add(message); }
    private void FlushNotices()
    {
        if(notices.Count==0)return;
        var messages=notices.Distinct().ToArray();
        notices.Clear();
        if(engine.NotificationsEnabled && !closing)
            tray?.ShowBalloonTip(4000,"Every Click Counts.",string.Join("\n",messages.Take(3))
                +(messages.Length>3?$"\n+{messages.Length-3} more highlights":""),Forms.ToolTipIcon.Info);
    }
    private void TogglePause()
    {
        if(!ready || trackingFailed)return;
        try { Drain(); engine.Paused=!engine.Paused; engine.Save(); Render(); FlushNotices(); }
        catch(Exception ex) { System.Windows.MessageBox.Show(ex.Message,"Unable to save"); }
    }
    private void PauseClick(object sender,RoutedEventArgs e)=>TogglePause();
    private void TrayClick(object sender,RoutedEventArgs e) { showcase?.Close(); WindowState=WindowState.Minimized; Hide(); }
    private void Restore() { Show(); WindowState=WindowState.Normal; Activate(); }
    private void SettingsChanged(object sender,RoutedEventArgs e)
    {
        if(!ready)return;
        engine.NotificationsEnabled=Notifications.IsChecked==true;
        engine.Metric=Metric.IsChecked==true;
        Render();
    }
    private void CalibrationChanged(object sender,RoutedEventArgs e)
    {
        if(!ready)return;
        if(double.TryParse(Calibration.Text,out double ppi)&&double.IsFinite(ppi)&&ppi>=1&&ppi<=10000)
            engine.PixelsPerInch=ppi;
        Calibration.Text=engine.PixelsPerInch.ToString(CultureInfo.CurrentCulture);
        lastFactAt=double.NegativeInfinity;
        Render();
    }
    private void CalibrationStepClick(object sender,RoutedEventArgs e)
    {
        if(!ready)return;
        CalibrationChanged(Calibration,e);
        int step=int.Parse((string)((Controls.Button)sender).Tag,CultureInfo.InvariantCulture);
        Calibration.Text=Math.Clamp(engine.PixelsPerInch+step,1,10000).ToString(CultureInfo.CurrentCulture);
        CalibrationChanged(Calibration,e);
    }
    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        string? capture=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--capture="));
        if(capture==null)return;
        var shot=new DispatcherTimer { Interval=TimeSpan.FromSeconds(2) };
        shot.Tick+=(_,_)=> {
            shot.Stop();
            try
            {
                var surface=(FrameworkElement)Content;
                var dpi=Media.VisualTreeHelper.GetDpi(this);
                var bitmap=new Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(surface.ActualWidth*dpi.DpiScaleX),
                    (int)Math.Ceiling(surface.ActualHeight*dpi.DpiScaleY),dpi.PixelsPerInchX,dpi.PixelsPerInchY,Media.PixelFormats.Pbgra32);
                bitmap.Render(surface);
                var encoder=new Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(Media.Imaging.BitmapFrame.Create(bitmap));
                using var file=File.Create(capture[10..]);
                encoder.Save(file);
            }
            catch(Exception ex) { SaveText.Text="Dashboard capture failed: "+ex.Message; }
        };
        shot.Start();
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if(closing)return;
        closing=true;
        timer.Stop(); hooks?.Dispose(); showcase?.Close();
        try { if(ready) { if(!trackingFailed)Drain(); engine.Save(); } }
        catch(Exception ex) { System.Windows.MessageBox.Show("Final save failed: "+ex.Message); }
        tray?.Dispose(); store?.Dispose(); instance?.Dispose();
        base.OnClosing(e);
    }
}
