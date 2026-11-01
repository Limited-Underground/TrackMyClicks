// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using TrackMyClicks;

// A diagnostic benchmark, not a numerical pass/fail budget. Foreground state matters:
// this host also exhibits high foreground WPF CPU with the preserved V0.2 binary.
internal static class Program
{
    [STAThread] static int Main(string[] args)
    {
        string root=System.IO.Path.GetFullPath(args.FirstOrDefault()??".");
        string evidence=System.IO.Path.Combine(root,"evidence","v03","performance");
        System.IO.Directory.CreateDirectory(evidence);
        Environment.SetEnvironmentVariable("TRACKMYCLICKS_DATA_DIR",System.IO.Path.Combine(evidence,"profile-"+Guid.NewGuid().ToString("N")));
        var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
        var window=new MainWindow();
        var rows=new List<object>();
        var steps=new Queue<(string Name,int Seconds,Action Setup)>();
        steps.Enqueue(("normal Application.Run tracking",15,()=>{}));
        steps.Enqueue(("normal lifetime, pause button focused",10,()=>((UIElement)window.FindName("PauseButton")).Focus()));
        if(args.Contains("--isolate"))
        {
            steps.Enqueue(("timer stopped",6,()=>((DispatcherTimer)typeof(MainWindow).GetField("timer",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!).Stop()));
            steps.Enqueue(("timer and hooks stopped",6,()=>((IDisposable)typeof(MainWindow).GetField("hooks",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(window)!).Dispose()));
            steps.Enqueue(("window hidden",6,()=>window.Hide()));
        }
        var timer=new DispatcherTimer{Interval=TimeSpan.FromSeconds(3)};
        var process=Process.GetCurrentProcess();
        var watch=new Stopwatch();
        (string Name,int Seconds,Action Setup)? current=null;
        double before=0;
        int result=0;
        timer.Tick+=(_,_) =>
        {
            try
            {
                if(current.HasValue)
                {
                    process.Refresh();
                    var row=new
                    {
                        name=current.Value.Name,elapsedSeconds=watch.Elapsed.TotalSeconds,isActive=window.IsActive,
                        cpuCorePercent=(process.TotalProcessorTime.TotalMilliseconds-before)/watch.Elapsed.TotalMilliseconds*100,
                        workingSetMB=process.WorkingSet64/1048576d,privateMB=process.PrivateMemorySize64/1048576d
                    };
                    rows.Add(row);
                    Console.WriteLine(JsonSerializer.Serialize(row));
                    System.IO.File.WriteAllText(System.IO.Path.Combine(evidence,"normal-lifetime.json"),
                        JsonSerializer.Serialize(rows,new JsonSerializerOptions{WriteIndented=true}));
                }
                if(steps.Count==0){timer.Stop();window.Close();app.Shutdown();return;}
                current=steps.Dequeue();
                current.Value.Setup();
                process.Refresh();before=process.TotalProcessorTime.TotalMilliseconds;watch.Restart();
                timer.Interval=TimeSpan.FromSeconds(current.Value.Seconds);
            }
            catch(Exception ex){Console.Error.WriteLine(ex);result=1;timer.Stop();window.Close();app.Shutdown();}
        };
        timer.Start();
        app.Run(window);
        return result;
    }
}
