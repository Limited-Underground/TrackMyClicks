// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TrackMyClicks;
using TrackMyClicks.Core;
using Path = System.IO.Path;
using File = System.IO.File;
using Directory = System.IO.Directory;

internal static class Program
{
    private static int checks;
    private static readonly List<string> results = new();
    private static string evidence = "";
    private static void Assert(bool condition, string name)
    {
        if(!condition) throw new InvalidOperationException("FAIL: " + name);
        checks++; results.Add("PASS: " + name); Console.WriteLine(results[^1]);
    }
    private static T Field<T>(object target, string name) => (T)(target.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public)?.GetValue(target) ?? throw new InvalidOperationException("Missing field " + name));
    private static void Invoke(object target,string name) => target.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic,null,Type.EmptyTypes,null)!.Invoke(target,null);
    private static T Control<T>(Window window,string name) where T:FrameworkElement => (T)(window.FindName(name) ?? throw new InvalidOperationException("Missing control " + name));
    private static void Pump()
    {
        var frame=new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));
        Dispatcher.PushFrame(frame);
    }
    private static void Capture(Window window,string file,double scale=1)
    {
        window.UpdateLayout(); Pump();
        var content=(FrameworkElement)window.Content;
        int width=(int)Math.Ceiling(content.ActualWidth*scale), height=(int)Math.Ceiling(content.ActualHeight*scale);
        var bitmap=new RenderTargetBitmap(width,height,96*scale,96*scale,PixelFormats.Pbgra32);
        bitmap.Render(content);
        var encoder=new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream=File.Create(Path.Combine(evidence,file)); encoder.Save(stream);
    }
    [STAThread]
    public static int Main(string[] args)
    {
        evidence=Path.GetFullPath(args.FirstOrDefault() ?? Path.Combine("evidence","v03","ui-tests"));
        Directory.CreateDirectory(evidence);
        string data=Path.Combine(evidence,"ui-test-data",DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
        Environment.SetEnvironmentVariable("TRACKMYCLICKS_DATA_DIR",data);
        Application? app=null; MainWindow? window=null;
        try
        {
            // Presentation tests do not request or alter any tracking engine behavior.
            var clicks=new AchievementCard(new("click100","First century","100 mouse clicks",false));
            var totals=new Totals { LeftClicks=10,RightClicks=15,MiddleClicks=25 };
            clicks.Update(new("click100","First century","100 mouse clicks",false),totals,96);
            Assert(clicks.ProgressPercent==50 && clicks.ProgressText.Contains("50") && !clicks.IsUnlocked,"click card aggregates all mouse buttons");
            totals.LeftClicks=60; clicks.Update(new("click100","First century","100 mouse clicks",true),totals,96);
            Assert(clicks.ProgressPercent==100 && clicks.IsUnlocked && clicks.PercentText=="Completed","completed card uses persisted unlock state");
            totals.LeftClicks=500; clicks.Update(new("click100","First century","100 mouse clicks",true),totals,96);
            Assert(clicks.ProgressPercent==100 && clicks.ProgressText=="100 / 100","completed card clamps display above target");
            var move=new AchievementCard(new("move100","On the move","100m",false));
            totals.DistancePixels=100*96/.0254;
            move.Update(new("move100","On the move","100m",false),totals,192);
            Assert(Math.Abs(move.ProgressPercent-50)<.0001,"movement card respects192 PPI calibration");
            move.Update(new("move100","On the move","100m",true),totals,192);
            Assert(move.IsUnlocked && move.ProgressPercent==100,"recalibration does not visually revoke earned badge");
            var keys=new AchievementCard(new("key1000","Keyboard warm-up","1,000 keys",false));
            totals.KeyPresses=250; keys.Update(new("key1000","Keyboard warm-up","1,000 keys",false),totals,96);
            Assert(keys.ProgressPercent==25,"keyboard card target and progress");
            int changes=0; keys.PropertyChanged+=(_,_)=>changes++;
            keys.Update(new("key1000","Keyboard warm-up","1,000 keys",false),totals,96);
            Assert(changes==0,"unchanged progress emits no redraw notifications");

            app=new Application { ShutdownMode=ShutdownMode.OnExplicitShutdown };
            window=new MainWindow();
            var engine=Field<ActivityEngine>(window,"engine");
            if(engine is null) throw new InvalidOperationException("WPF engine not initialized");
            Assert(true,"actual WPF window constructs with SQLite and hooks");
            // Stop external inputs after proving hook installation; tests below target deterministic UI behavior.
            Field<DispatcherTimer>(window,"timer").Stop();
            ((IDisposable)Field<object>(window,"hooks")).Dispose();
            window.Width=1710; window.Height=919; window.Show(); Pump();
            Assert(Control<ItemsControl>(window,"AchievementCards").Items.Count==3,"default milestone view shows three cards");
            Control<Button>(window,"ShowAllButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
            var detail=Field<AchievementShowcase>(window,"showcase");
            Assert(detail.IsVisible && Control<ItemsControl>(detail,"CategoryGroups").Items.Count==10,"Show all opens the dedicated showcase with all ten categories");
            Assert(Control<ItemsControl>(window,"AchievementCards").Items.Count==3,"showcase preserves the existing three-card milestone preview");
            Capture(detail,"showcase-initial.png");
            Control<Button>(detail,"BackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); Pump();
            Assert(!detail.IsVisible && window.IsVisible,"Back to dashboard closes showcase and returns to dashboard");
            bool before=engine.Paused;
            Control<Button>(window,"PauseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert(engine.Paused!=before,"pause button routes to existing tracking state");
            Control<Button>(window,"PauseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert(engine.Paused==before,"resume button restores tracking state");
            engine.Paused=false; engine.Click(TrackMyClicks.Core.MouseButton.Left); engine.Click(TrackMyClicks.Core.MouseButton.Right); engine.Click(TrackMyClicks.Core.MouseButton.Middle); engine.KeyPress(); engine.Move(96*12); engine.Scroll(120);
            Invoke(window,"Render");
            Assert(Control<TextBlock>(window,"LeftToday").Text=="1" && Control<TextBlock>(window,"RightToday").Text=="1" && Control<TextBlock>(window,"MiddleToday").Text=="1" && Control<TextBlock>(window,"KeysToday").Text=="1","activity table renders all original count categories");
            var metric=Control<CheckBox>(window,"Metric"); metric.IsChecked=true;
            Assert(engine.Metric && Control<TextBlock>(window,"DistanceToday").Text.Contains("m"),"metric control retains distance conversion");
            var notifications=Control<CheckBox>(window,"Notifications"); notifications.IsChecked=false;
            Assert(!engine.NotificationsEnabled,"notification control retains setting behavior");
            var calibration=Control<TextBox>(window,"Calibration"); calibration.Text="192"; calibration.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            Assert(engine.PixelsPerInch==192,"calibration control retains valid value behavior");
            Control<Button>(window,"CalibrationIncrease").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert(engine.PixelsPerInch==193 && calibration.Text=="193","calibration increase spin button reuses validation semantics");
            Control<Button>(window,"CalibrationDecrease").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert(engine.PixelsPerInch==192 && calibration.Text=="192","calibration decrease spin button reuses validation semantics");
            calibration.Text="invalid"; calibration.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            Assert(engine.PixelsPerInch==192 && calibration.Text=="192","invalid calibration is rejected without changing tracking");
            metric.IsChecked=false; calibration.Text="96"; calibration.RaiseEvent(new RoutedEventArgs(UIElement.LostFocusEvent));
            Assert(Control<ItemsControl>(window,"DailyChallengeCards").Items.Count==3,"dashboard presents three daily challenges");
            Assert(Control<ItemsControl>(window,"PersonalRecordCards").Items.Count==6,"dashboard presents six dated personal records");
            Assert(Control<TextBlock>(window,"StreakCurrentText").Text=="1 day","dashboard displays actual current activity streak");
            Assert(Control<TextBlock>(window,"ClickRateText").Text=="3" && Control<TextBlock>(window,"ClickRateBestText").Text=="3","speedometer displays observed clicks without startup extrapolation");
            Assert(Enum.GetValues<AchievementTier>().Select(t=>ExpansionPalette.Hex(t.ToString())).Distinct().Count()==5,"all five badge tiers have distinct native colors");
            string unchangedFact=Control<TextBlock>(window,"FunnyFactText").Text;
            Invoke(window,"Render"); Assert(Control<TextBlock>(window,"FunnyFactText").Text==unchangedFact,"facts do not churn on every dashboard render");
            Capture(window,"dashboard-1710x919.png");
            long expectedClicks=3,expectedKeys=1; double expectedScroll=1;
            var goal=engine.Insights.DailyChallenges.First(c=>c.Metric!=InsightMetric.ClickRate);
            double remaining=Math.Max(0,goal.Target-goal.Current);
            switch(goal.Metric)
            {
                case InsightMetric.Clicks: long clickDelta=(long)Math.Ceiling(remaining); for(long n=0;n<clickDelta;n++)engine.Click(TrackMyClicks.Core.MouseButton.Left); expectedClicks+=clickDelta; break;
                case InsightMetric.Keyboard: long keyDelta=(long)Math.Ceiling(remaining); for(long n=0;n<keyDelta;n++)engine.KeyPress(); expectedKeys+=keyDelta; break;
                case InsightMetric.MovementMeters: engine.Move(remaining*engine.PixelsPerInch/.0254+1); break;
                case InsightMetric.Scroll: int delta=(int)Math.Ceiling(remaining*120);engine.Scroll(delta);expectedScroll+=delta/120d;break;
                default:throw new InvalidOperationException("Unexpected challenge metric");
            }
            Invoke(window,"Render"); Pump();
            var goalCard=Control<ItemsControl>(window,"DailyChallengeCards").Items.Cast<ExpansionCard>().Single(c=>c.Id==goal.Id);
            Assert(goalCard.IsUnlocked && goalCard.ProgressPercent==100 && goalCard.DetailText.Contains("25 points"),"real tracked activity completes the visible challenge and its reward state");
            Assert(engine.Insights.TotalPoints==25 && Control<TextBlock>(window,"RewardPointsText").Text=="25","dashboard reflects exactly one durable challenge reward");
            var viewportLower=(ScrollViewer)window.Content;viewportLower.ScrollToBottom();Pump();Capture(window,"dashboard-challenges-records-facts.png");viewportLower.ScrollToTop();Pump();
            Control<Button>(window,"ShowAllButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();
            detail=Field<AchievementShowcase>(window,"showcase");
            Assert(Control<ItemsControl>(detail,"ChallengeItems").Items.Cast<ExpansionCard>().Any(c=>c.Id==goal.Id && c.IsUnlocked),"showcase includes completed daily challenge receipts");
            Assert(Control<TextBlock>(detail,"SummaryText").Text.Contains("25 points"),"showcase lifetime summary includes earned points");
            Capture(detail,"showcase-awards.png");
            var detailScroll=Control<ScrollViewer>(detail,"ShowcaseScroll");detailScroll.ScrollToBottom();Pump();Capture(detail,"showcase-streak-diamond.png");
            detail.Width=800;detail.Height=650;Pump();Capture(detail,"showcase-narrow.png");
            Assert(Control<FrameworkElement>(detail,"ShowcaseRoot").ActualWidth>0,"dedicated achievement showcase resizes with scrollable content");
            Control<Button>(detail,"BackButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Pump();
            var firstCard=Field<List<AchievementCard>>(window,"achievementCards")[0];
            firstCard.Update(new Achievement(firstCard.Id,firstCard.Title,firstCard.Description,true),engine.Lifetime,engine.PixelsPerInch); Pump();
            var presenter=(ContentPresenter)Control<ItemsControl>(window,"AchievementCards").ItemContainerGenerator.ContainerFromIndex(0);
            var surface=(Border)presenter.ContentTemplate.FindName("AchievementSurface",presenter);
            Assert(surface.Background is SolidColorBrush completedBrush && completedBrush.Color==Color.FromRgb(23,59,56),"earned achievement applies distinct completed card styling");
            Capture(window,"completed-achievement.png");
            Invoke(window,"Render");
            window.Width=1000; window.Height=760; Pump(); Capture(window,"dashboard-1000x760.png");
            Assert(Grid.GetRow(Control<FrameworkElement>(window,"RightStack"))==1,"narrow layout stacks settings and milestones below activity");
            window.Width=900; window.Height=700; Pump(); Capture(window,"dashboard-900x700.png");
            Assert(window.ActualWidth>=900 && window.ActualHeight>=700,"minimum requested window size remains available");
            window.Width=1710; window.Height=919; Pump();
            // Simulate a1710x919 physical viewport at scaled Windows DPI by shrinking logical dimensions.
            // The test lowers MinHeight only to stress scrolling below the product's supported minimum.
            window.MinWidth=0; window.MinHeight=0;
            foreach(double dpiScale in new[]{1.25,1.5})
            {
                window.Width=1710/dpiScale; window.Height=919/dpiScale; Pump();
                Invoke(window,"UpdateResponsiveLayout"); window.UpdateLayout();
                var root=Control<FrameworkElement>(window,"RootLayout");
                var viewport=(ScrollViewer)window.Content;
                Assert(root.ActualWidth<=viewport.ViewportWidth+.5,"scaled logical layout fits viewport at"+dpiScale);
                Capture(window,dpiScale==1.25?"dashboard-dpi125.png":"dashboard-dpi150.png",dpiScale);
            }
            window.Width=1710; window.Height=919; window.MinWidth=900; window.MinHeight=700; Pump();
            Assert(File.Exists(Path.Combine(evidence,"dashboard-dpi125.png")) && File.Exists(Path.Combine(evidence,"dashboard-dpi150.png")),"vector WPF rendering captured at125% and150% DPI");
            window.GetType().GetMethod("TrayClick",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(window,new object?[]{window,new RoutedEventArgs()}); Pump();
            Assert(!window.IsVisible,"minimize-to-tray retains hide behavior");
            Invoke(window,"Restore"); Pump();
            Assert(window.IsVisible && window.WindowState==WindowState.Normal,"tray restore method restores normal visible window");
            engine.Paused=true; Invoke(window,"Render"); window.Close(); window=null;
            using(var store=new Store(Path.Combine(data,"activity.sqlite3")))
            {
                var restored=new ActivityEngine(store);
                Assert(restored.Lifetime.Clicks==expectedClicks && restored.Lifetime.KeyPresses==expectedKeys && Math.Abs(restored.Lifetime.ScrollUnits-expectedScroll)<.0001,"redesigned window shutdown preserves all tracked aggregates");
                Assert(restored.Paused && !restored.NotificationsEnabled && !restored.Metric && restored.PixelsPerInch==96,"redesigned settings and pause survive SQLite restart");
                Assert(restored.Insights.TotalPoints==25 && restored.Insights.CompletedChallenges.Any(c=>c.Id==goal.Id),"visible challenge reward and receipt survive real window close and SQLite restart");
            }
            Console.WriteLine($"{checks} WPF presentation/integration checks passed.");
            return 0;
        }
        catch(TargetInvocationException ex) { return Fail(ex.InnerException ?? ex); }
        catch(Exception ex) { return Fail(ex); }
        finally
        {
            try { window?.Close(); } catch(Exception ex) { results.Add("Close failed: "+ex.Message); }
            app?.Shutdown();
            File.WriteAllLines(Path.Combine(evidence,"results.txt"),results.Append($"{checks} checks passed."));
        }
    }
    private static int Fail(Exception ex) { Console.Error.WriteLine(ex); results.Add(ex.ToString()); return 1; }
}





