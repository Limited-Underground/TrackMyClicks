// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Reflection;
using System.Runtime.Loader;

internal static class LegacyProfile
{
    // This executes the actual shipped V0.2 Core assembly in a separate load context.
    // It does not copy or open the owner's database.
    public static void Create(string legacyAssemblyPath, string databasePath, DateTime day)
    {
        var context = new AssemblyLoadContext("TrackMyClicks.V02Fixture." + Guid.NewGuid().ToString("N"), true);
        object? store = null;
        try
        {
            Assembly assembly = context.LoadFromAssemblyPath(Path.GetFullPath(legacyAssemblyPath));
            Type storeType = assembly.GetType("TrackMyClicks.Core.Store", true)!;
            Type engineType = assembly.GetType("TrackMyClicks.Core.ActivityEngine", true)!;
            Type buttonType = assembly.GetType("TrackMyClicks.Core.MouseButton", true)!;
            store = Activator.CreateInstance(storeType, databasePath)!;
            var engine = Activator.CreateInstance(engineType, store, new Func<DateTime>(() => day))!;
            MethodInfo click = engineType.GetMethod("Click", [buttonType])!;
            for (int i = 0; i < 100; i++) click.Invoke(engine, [Enum.ToObject(buttonType, i % 3)]);
            for (int i = 0; i < 1_000; i++) engineType.GetMethod("KeyPress")!.Invoke(engine, null);
            engineType.GetMethod("Move")!.Invoke(engine, [100 * 96 / .0254]);
            engineType.GetMethod("Scroll")!.Invoke(engine, [240]);
            engineType.GetProperty("NotificationsEnabled")!.SetValue(engine, false);
            engineType.GetProperty("Metric")!.SetValue(engine, true);
            engineType.GetProperty("PixelsPerInch")!.SetValue(engine, 110d);
            engineType.GetProperty("Paused")!.SetValue(engine, true);
            engineType.GetMethod("Save")!.Invoke(engine, null);
        }
        finally
        {
            (store as IDisposable)?.Dispose();
            context.Unload();
        }
    }
}
