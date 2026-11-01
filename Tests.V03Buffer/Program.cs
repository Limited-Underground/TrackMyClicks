// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using TrackMyClicks;
using TrackMyClicks.Core;
using Directory = System.IO.Directory;
using File = System.IO.File;
using Path = System.IO.Path;

internal static class Program
{
    private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    private static readonly List<string> Results = new();
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + name);
        Results.Add("PASS: " + name); Console.WriteLine(Results[^1]);
    }
    private static object Field(object target, string name) => target.GetType().GetField(name, Flags)!.GetValue(target)!;
    private static void SetField(object target, string name, object value) => target.GetType().GetField(name, Flags)!.SetValue(target, value);
    private static object? Invoke(object target, string name, params object?[] args)
        => target.GetType().GetMethods(Flags).Single(m => m.Name == name && m.GetParameters().Length == args.Length).Invoke(target, args);
    private static void Pump()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
    private static Type Nested(string name) => typeof(MainWindow).GetNestedType(name, BindingFlags.NonPublic)!;
    private static object Click(MouseButton button, DateTime at, double seconds)
        => Activator.CreateInstance(Nested("ObservedClick"), button, at, seconds)!;
    private static object Batch(DateTime at, (MouseButton Button, DateTime At, double Seconds)[] clicks, long keys = 0, double distance = 0, int scroll = 0)
    {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(Nested("ObservedClick")))!;
        foreach (var click in clicks) list.Add(Click(click.Button, click.At, click.Seconds));
        return Activator.CreateInstance(Nested("BufferedInput"), at, list, keys, distance, scroll)!;
    }
    [STAThread]
    public static int Main(string[] args)
    {
        string root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
        string evidence = Path.Combine(root, "evidence", "v03", "buffer-integration");
        string profile = Path.Combine(evidence, "profiles", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(profile);
        Environment.SetEnvironmentVariable("TRACKMYCLICKS_DATA_DIR", profile);
        Application? app = null; MainWindow? window = null;
        try
        {
            app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            window = new MainWindow();
            ((DispatcherTimer)Field(window, "timer")).Stop();
            object hooks = Field(window, "hooks");
            ((IDisposable)hooks).Dispose();
            window.Show(); Pump();
            var store = (Store)Field(window, "store");
            double seconds = 501;
            var engine = new ActivityEngine(store, () => (DateTime)Field(window, "trackingTime"), () => seconds);
            SetField(window, "engine", engine);
            engine.Paused = true;
            ((Action<int>)Field(hooks, "Click"))(0);
            ((Action)Field(hooks, "Key"))();
            ((Action<double>)Field(hooks, "Move"))(123);
            ((Action<int>)Field(hooks, "Scroll"))(120);
            Check(((IList)Field(window, "observedClicks")).Count == 0 && (long)Field(window, "keys") == 0
                && (double)Field(window, "distance") == 0 && (int)Field(window, "scroll") == 0,
                "actual hook event handlers exclude every input category from the producer buffer while paused");
            Check(engine.Lifetime.Clicks == 0 && engine.Insights.CurrentStreak == 0 && engine.Insights.TotalPoints == 0,
                "paused producer events cannot establish totals, streaks or rewards");
            engine.Paused = false;
            DateTime beforeMidnight = new(2026, 10, 29, 23, 59, 59);
            DateTime afterMidnight = new(2026, 10, 30, 0, 0, 0);
            Invoke(window, "Apply", Batch(beforeMidnight,
                [(MouseButton.Left, beforeMidnight, 500), (MouseButton.Right, beforeMidnight.AddMilliseconds(500), 500.5)], 2, 96, 120));
            Invoke(window, "Apply", Batch(afterMidnight, [(MouseButton.Middle, afterMidnight, 501)], 1));
            engine.Save();
            Check(store.ReadTotals("2026-10-29").Clicks == 2 && store.ReadTotals("2026-10-29").KeyPresses == 2
                && store.ReadTotals("2026-10-30").Clicks == 1 && store.ReadTotals("2026-10-30").KeyPresses == 1,
                "real WPF buffered replay preserves both sides of the local midnight boundary");
            Check(engine.Lifetime.Clicks == 3 && engine.Lifetime.KeyPresses == 3 && engine.Lifetime.DistancePixels == 96 && engine.Lifetime.ScrollUnits == 1,
                "replaying original WPF aggregates retains all original lifetime categories");
            Check(engine.Insights.CurrentClicksPerMinute == 3 && engine.Insights.TodayPeakClicksPerMinute == 1,
                "rolling rate spans midnight but new-day rate challenge peak excludes yesterday's clicks");
            seconds = 560.499; engine.RefreshInsights();
            Check(engine.Insights.CurrentClicksPerMinute == 2, "WPF replay preserves first observation's original expiry rather than assigning drain time");
            seconds = 560.5; engine.RefreshInsights();
            Check(engine.Insights.CurrentClicksPerMinute == 1, "WPF replay preserves exact sixty-second expiry for an observation inside one buffered batch");
            seconds = 561; engine.RefreshInsights();
            Check(engine.Insights.CurrentClicksPerMinute == 0, "every original click ages out independently after WPF replay");

            seconds = 1_000;
            DateTime delayed = afterMidnight.AddMinutes(10);
            Invoke(window, "Apply", Batch(delayed,
                [(MouseButton.Left, delayed, 930), (MouseButton.Left, delayed.AddMilliseconds(100), 930.1),
                 (MouseButton.Right, delayed.AddMilliseconds(200), 930.2), (MouseButton.Middle, delayed.AddMilliseconds(300), 930.3)]));
            Check(engine.Insights.CurrentClicksPerMinute == 0 && engine.Insights.HighestClicksPerMinute == 4,
                "seventy-second WPF scheduling delay cannot inflate live rate and does preserve the actual observed historical best");

            var producer = (IList)Field(window, "observedClicks");
            producer.Add(Click(MouseButton.Left, delayed, 1_000));
            SetField(window, "keys", 3L); SetField(window, "distance", 11d); SetField(window, "scroll", 120);
            object detached = Invoke(window, "TakeBuffer")!;
            var detachedClicks = (IList)detached.GetType().GetProperty("Clicks")!.GetValue(detached)!;
            Check(detachedClicks.Count == 1 && !ReferenceEquals(producer, Field(window, "observedClicks"))
                && ((IList)Field(window, "observedClicks")).Count == 0 && (long)Field(window, "keys") == 0,
                "TakeBuffer transfers ownership and clears producer counters before any engine or database work");

            // Force a real SQLite failure at the first new-day save during Drain.
            // A detached batch must not remain available for accidental shutdown replay.
            SetField(window, "bufferDay", afterMidnight.AddDays(1));
            ((IList)Field(window, "observedClicks")).Add(Click(MouseButton.Left, afterMidnight.AddDays(1), 1_000));
            long savedClicks = engine.Lifetime.Clicks;
            using (var raw = new RawDb(Path.Combine(profile, "activity.sqlite3")))
                raw.Exec("CREATE TRIGGER independent_fail_state BEFORE INSERT ON expansion_state BEGIN SELECT RAISE(ABORT,'independent buffered failure'); END;");
            bool failed = false;
            try { Invoke(window, "Drain"); }
            catch (TargetInvocationException ex) when (ex.InnerException is System.IO.IOException) { failed = true; }
            Check(failed && ((IList)Field(window, "observedClicks")).Count == 0 && engine.Lifetime.Clicks == savedClicks,
                "actual SQLite failure during new-day buffered drain leaves no replayable producer batch or fabricated click");
            using (var raw = new RawDb(Path.Combine(profile, "activity.sqlite3"))) raw.Exec("DROP TRIGGER independent_fail_state;");
            SetField(window, "trackingFailed", true);
            window.Close(); window = null;
            using (var reopened = new Store(Path.Combine(profile, "activity.sqlite3")))
                Check(reopened.ReadTotals("lifetime").Clicks == savedClicks,
                    "shutdown after failed drain does not replay detached observations into SQLite");
            File.WriteAllLines(Path.Combine(evidence, "results.txt"), Results.Append($"{Results.Count} checks passed.").Append("Disposable profile: " + profile));
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            File.WriteAllLines(Path.Combine(evidence, "results.txt"), Results.Append(ex.ToString()).Append("Disposable profile: " + profile));
            return 1;
        }
        finally { try { window?.Close(); } finally { app?.Shutdown(); } }
    }
    private sealed class RawDb : IDisposable
    {
        private nint db;
        public RawDb(string path) { if (sqlite3_open(Encoding.UTF8.GetBytes(path + '\0'), out db) != 0) throw new System.IO.IOException("Cannot open disposable test database."); }
        public void Exec(string sql)
        {
            int code = sqlite3_exec(db, Encoding.UTF8.GetBytes(sql + '\0'), 0, 0, out nint error);
            if (code == 0) return;
            string message = Marshal.PtrToStringUTF8(error) ?? "SQLite test error"; sqlite3_free(error); throw new System.IO.IOException(message);
        }
        public void Dispose() { if (db != 0) sqlite3_close(db); db = 0; }
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_open(byte[] path, out nint db);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_exec(nint db, byte[] sql, nint callback, nint state, out nint error);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int sqlite3_close(nint db);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void sqlite3_free(nint pointer);
    }
}
