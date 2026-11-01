// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using TrackMyClicks.Core;

string projectRoot = Path.GetFullPath(args.FirstOrDefault() ?? ".");
string evidence = Path.Combine(projectRoot, "evidence", "v03", "independent");
string fixtures = Path.Combine(evidence, "profiles", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(fixtures);
var results = new List<string>();
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException("FAIL: " + name);
    string line = "PASS: " + name; results.Add(line); Console.WriteLine(line);
}
string Profile(string name) => Path.Combine(fixtures, name + ".sqlite3");

try
{
    // An authentic pre-upgrade fixture exercises the shipped V0.2 implementation,
    // rather than relying only on a test-authored approximation of its schema.
    string legacy = Profile("authentic-v02");
    DateTime legacyDay = new(2026, 10, 28, 10, 0, 0);
    LegacyProfile.Create(Path.Combine(projectRoot, "output", "TrackMyClicks-v0.2-win-x64", "TrackMyClicks.Core.dll"), legacy, legacyDay);
    DateTime local = legacyDay.AddDays(1); double seconds = 1_000;
    using (var store = new Store(legacy))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        Check(store.SchemaVersion == 1 && store.MigrationBackupPath is not null && File.Exists(store.MigrationBackupPath), "actual shipped V0.2 profile gets a preserved pre-upgrade SQLite backup");
        Check(engine.Lifetime.Clicks == 100 && engine.Lifetime.KeyPresses == 1_000 && engine.Lifetime.ScrollUnits == 2, "authentic V0.2 upgrade retains original aggregate totals");
        Check(engine.Achievements.Where(a => a.Unlocked).Select(a => a.Id).ToHashSet().SetEquals(new[] { "click100", "key1000", "move100" }), "authentic upgrade retains all previously unlocked legacy badges");
        Check(engine.Paused && !engine.NotificationsEnabled && engine.Metric && engine.PixelsPerInch == 110, "authentic upgrade retains pause and every original setting");
        Check(engine.Insights.CurrentStreak == 1 && engine.Insights.LongestStreak == 1, "historical activity imports streak with yesterday grace after closed application");
        Check(engine.Insights.Records.Single(r => r.Id == "clicks").Value == 100 && engine.Insights.Records.Single(r => r.Id == "clicks").Date == legacyDay.Date, "personal record backfill retains actual local activity date");
        engine.Save();
    }

    local = new(2026, 10, 29, 12, 0, 0); seconds = 100;
    string rateProfile = Profile("rate-boundaries");
    using (var store = new Store(rateProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        Check(engine.Insights.CurrentClicksPerMinute == 0 && engine.Insights.HighestClicksPerMinute == 0, "new profile starts at zero rather than extrapolating startup rate");
        engine.Click(MouseButton.Left, 100);
        Check(engine.Insights.CurrentClicksPerMinute == 1 && engine.Insights.HighestClicksPerMinute == 1, "one observed click means one click per rolling minute");
        seconds = 159.999; engine.Insights.Refresh();
        Check(engine.Insights.CurrentClicksPerMinute == 1, "click remains inside rolling window just before sixty seconds");
        seconds = 160; engine.Insights.Refresh();
        Check(engine.Insights.CurrentClicksPerMinute == 0 && engine.Insights.HighestClicksPerMinute == 1, "exact sixty-second boundary expires click and preserves best");
        seconds = 200; engine.Click(MouseButton.Right, 201); engine.Click(MouseButton.Middle, 139);
        Check(engine.Today.Clicks == 3 && engine.Insights.CurrentClicksPerMinute == 0, "invalid future and expired timing samples do not inflate live rate or suppress actual totals");
        engine.Click(MouseButton.Left, seconds); engine.Paused = true; long before = engine.Lifetime.Clicks;
        engine.Click(MouseButton.Left, seconds); engine.KeyPress(); engine.Move(100); engine.Scroll(120);
        Check(engine.Lifetime.Clicks == before && engine.Lifetime.KeyPresses == 0 && engine.Lifetime.DistancePixels == 0 && engine.Lifetime.ScrollUnits == 0, "paused input cannot affect rate, activity totals, records or streak participation");
        seconds = 261; engine.Insights.Refresh();
        Check(engine.Insights.CurrentClicksPerMinute == 0, "rolling rate ages out while tracking is paused");
        engine.Paused = false; engine.Click(MouseButton.Left, seconds);
        Check(engine.Insights.CurrentClicksPerMinute == 1, "resume does not include paused or expired clicks");
        local = local.AddHours(-1); seconds = 270;
        engine.Click(MouseButton.Left, seconds);
        Check(engine.Insights.CurrentClicksPerMinute == 2, "local wall-clock rollback does not alter monotonic rolling window");
        engine.Save();
    }
    using (var store = new Store(rateProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        Check(engine.Insights.CurrentClicksPerMinute == 0 && engine.Insights.HighestClicksPerMinute >= 2, "restart resets only transient speed window and preserves recorded personal best");
    }

    local = new(2026, 10, 29, 23, 59, 59); seconds = 1_000;
    string streakProfile = Profile("streak-rollover");
    using (var store = new Store(streakProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        engine.Scroll(0); engine.Move(0); engine.Move(double.NaN);
        Check(engine.Insights.CurrentStreak == 0, "zero and invalid movement or scroll never fabricate an active day");
        engine.Move(1);
        Check(engine.Insights.CurrentStreak == 1, "real pointer-only activity establishes an active day");
        local = local.AddSeconds(2); seconds += 2; engine.RefreshDay();
        Check(engine.Insights.CurrentStreak == 1 && engine.Today.DistancePixels == 0, "midnight rollover retains yesterday grace without copying daily aggregates");
        engine.Paused = true; engine.KeyPress(); engine.Scroll(120); engine.Save();
        Check(engine.Insights.CurrentStreak == 1, "paused next-day input cannot extend a streak");
    }
    using (var store = new Store(streakProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        Check(engine.Insights.CurrentStreak == 1 && engine.Paused, "closing and restarting does not itself reset an eligible current streak");
        engine.Paused = false; engine.Scroll(120);
        Check(engine.Insights.CurrentStreak == 2 && engine.Insights.LongestStreak == 2, "real scroll-only activity extends consecutive local days");
        local = local.AddDays(2); seconds += 172_800; engine.RefreshDay();
        Check(engine.Insights.CurrentStreak == 0 && engine.Insights.LongestStreak == 2, "missing an entire local calendar day ends current but retains longest streak");
        engine.KeyPress();
        Check(engine.Insights.CurrentStreak == 1 && engine.Insights.LongestStreak == 2, "new activity after a gap starts a new run without deleting record history");
        engine.Save();
    }

    using (var store = new Store(legacy))
    {
        var engine = new ActivityEngine(store, () => legacyDay.AddDays(1), () => seconds);
        Check(engine.Insights.ExpandedAchievements.Where(a => a.Id is "click100" or "key1000" or "move100")
            .All(a => a.Unlocked && a.LegacyUnknownDate && a.UnlockedAt is null), "authentic legacy unlocks keep unknown dates rather than fabricated migration timestamps");
    }
    Check(ExpansionRules.Achievements.Select(a => a.Id).Distinct().Count() == ExpansionRules.Achievements.Count,
        "central tier catalog has unique immutable achievement identifiers");
    Check(ExpansionRules.Achievements.Where(a => a.Scope == AchievementScope.Streak).Select(a => a.Target)
        .SequenceEqual(new double[] { 3, 7, 14, 30, 60, 100 }), "streak catalog includes every required three through one-hundred-day award");

    local = new(2026, 11, 2, 12, 0, 0); seconds = 5_000;
    string challengeProfile = Profile("challenge-atomic-restart");
    string[] assigned;
    Dictionary<string, double> assignedTargets;
    int savedPoints;
    var noticedChallenges = new List<string>();
    var noticedBadges = new List<string>();
    bool everyRewardDurable = true, everyBadgeDurable = true;
    using (var store = new Store(challengeProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        assigned = engine.Insights.DailyChallenges.Select(c => c.Id).ToArray();
        assignedTargets = engine.Insights.DailyChallenges.ToDictionary(c => c.RuleId, c => c.Target);
        Check(assigned.Length == 3 && engine.Insights.DailyChallenges.Select(c => c.Metric).Distinct().Count() == 3,
            "each local day offers exactly three distinct activity challenge categories");
        engine.Insights.ChallengeCompleted += challenge =>
        {
            noticedChallenges.Add(challenge.Id);
            using var verifier = new Store(challengeProfile);
            using var json = System.Text.Json.JsonDocument.Parse(verifier.ReadExpansionState()!);
            everyRewardDurable &= json.RootElement.GetProperty("ChallengeRewards").TryGetProperty(challenge.Id, out _);
        };
        engine.Insights.ExpandedAchievementUnlocked += achievement =>
        {
            noticedBadges.Add(achievement.Id);
            using var verifier = new Store(challengeProfile);
            using var json = System.Text.Json.JsonDocument.Parse(verifier.ReadExpansionState()!);
            everyBadgeDurable &= json.RootElement.GetProperty("Unlocks").TryGetProperty(achievement.Id, out _);
        };
        engine.Paused = true;
        for (int i = 0; i < 20; i++) engine.Click(MouseButton.Left, seconds);
        engine.KeyPress(); engine.Move(100); engine.Scroll(120);
        Check(engine.Insights.DailyChallenges.All(c => c.Current == 0 && !c.Completed) && engine.Insights.TotalPoints == 0,
            "paused activity cannot advance or reward any daily challenge");
        engine.Paused = false;
        for (int i = 0; i < 1_100; i++) engine.Click((MouseButton)(i % 3), seconds);
        for (int i = 0; i < 5_100; i++) engine.KeyPress();
        engine.Scroll(120 * 600); engine.Move(1_000 * 96 / .0254);
        Check(engine.Insights.DailyChallenges.All(c => c.Completed) && engine.Insights.ChallengeCompletions == 3,
            "actual observed test inputs complete all assigned challenge kinds including rate and estimated distance");
        Check(noticedChallenges.Count == 3 && noticedChallenges.Distinct().Count() == 3 && everyRewardDurable,
            "each challenge notifies exactly once only after its reward is visible in a separate SQLite connection");
        Check(noticedBadges.Count == noticedBadges.Distinct().Count() && everyBadgeDurable,
            "every tier unlock notifies once only after its durable SQLite commit");
        savedPoints = engine.Insights.TotalPoints;
        engine.Click(MouseButton.Left, seconds); engine.KeyPress(); engine.Scroll(120); engine.Move(1);
        Check(engine.Insights.TotalPoints == savedPoints && engine.Insights.ChallengeCompletions == 3,
            "subsequent input cannot duplicate points or challenge completion badges");
        engine.Save();
        using var json = System.Text.Json.JsonDocument.Parse(store.ReadExpansionState()!);
        var stateNames = json.RootElement.EnumerateObject().Select(p => p.Name).ToArray();
        Check(!stateNames.Any(n => n.Contains("Samples", StringComparison.OrdinalIgnoreCase) || n.Contains("Timestamp", StringComparison.OrdinalIgnoreCase)),
            "persisted expansion state excludes transient per-click timing samples");
    }
    // Simulate a future code release changing catalog targets after today's assignment
    // was committed. Existing assignment snapshots must remain stable.
    var pool = (ChallengeDefinition[])ExpansionRules.ChallengePool;
    var originalPool = pool.ToArray();
    try
    {
        for (int i = 0; i < pool.Length; i++) pool[i] = pool[i] with { Target = pool[i].Target * 2 };
        using var store = new Store(challengeProfile);
        var engine = new ActivityEngine(store, () => local, () => seconds);
        Check(engine.Insights.DailyChallenges.Select(c => c.Id).SequenceEqual(assigned)
            && engine.Insights.DailyChallenges.All(c => c.Target == assignedTargets[c.RuleId]),
            "persisted same-day assignments and targets survive restart even if the code catalog changes");
        Check(engine.Insights.TotalPoints == savedPoints && engine.Insights.CompletedChallenges.Count == 3
            && engine.Insights.DailyChallenges.All(c => c.Completed && c.CompletedAt.HasValue),
            "restarting restores once-only rewards, completion timestamps and point totals");
        int repeats = 0; engine.Insights.ChallengeCompleted += _ => repeats++;
        engine.Click(MouseButton.Left, seconds); engine.KeyPress(); engine.Scroll(120); engine.Move(1);
        Check(repeats == 0 && engine.Insights.TotalPoints == savedPoints,
            "activity after restart cannot re-announce or re-award completed daily challenges");
    }
    finally { Array.Copy(originalPool, pool, pool.Length); }

    using (var store = new Store(challengeProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        local = local.AddDays(1); seconds += 86_400; engine.RefreshDay();
        Check(engine.Insights.DailyChallenges.Count == 3 && engine.Insights.DailyChallenges.All(c => !assigned.Contains(c.Id) && c.Current == 0 && !c.Completed),
            "midnight offers a fresh dated challenge set without carrying completed progress into a new day");
        Check(engine.Insights.TotalPoints == savedPoints && engine.Insights.CompletedChallenges.Count == 3,
            "daily rollover retains lifetime challenge rewards and points");
        engine.Save();
    }

    string noticeProfile = Profile("record-notice-latches");
    local = new(2026, 11, 4, 10, 0, 0);
    using (var store = new Store(noticeProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        var notices = new List<string>(); engine.Insights.RecordEstablished += r => notices.Add(r.Id);
        engine.KeyPress(); engine.KeyPress(); engine.KeyPress(); engine.Save();
        Check(notices.Count(id => id == "keys") == 1, "personal best notification is throttled once per category and local day rather than each input");
    }
    using (var store = new Store(noticeProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        int repeats = 0; engine.Insights.RecordEstablished += r => { if (r.Id == "keys") repeats++; };
        engine.KeyPress();
        Check(repeats == 0, "record notification latch persists across application restarts");
        local = local.AddDays(1); engine.RefreshDay();
        for (int i = 0; i < 5; i++) engine.KeyPress();
        Check(repeats == 1 && engine.Insights.Records.Single(r => r.Id == "keys").Date == local.Date,
            "a genuinely higher record on a later day can notify once with its achieved date");
        engine.Save();
    }

    string futureProfile = Profile("future-expansion-state");
    using (var store = new Store(futureProfile))
    {
        const string json = "{\"Version\":99}";
        store.Save("2026-11-05", new() { LeftClicks = 7 }, new() { LeftClicks = 7 }, ["click100"], false, true, false, 96, json);
        bool rejected = false;
        try { _ = new ActivityEngine(store, () => new DateTime(2026, 11, 5), () => seconds); }
        catch (IOException) { rejected = true; }
        Check(rejected && store.ReadExpansionState() == json && store.ReadTotals("lifetime").Clicks == 7 && store.ReadAchievements().Contains("click100"),
            "unsupported future expansion JSON fails safely without resetting totals, unlocks or state");
    }
    using (var store = new Store(Profile("facts-new-user")))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        Check(engine.Insights.GetFunnyFact(0).Contains("No activity", StringComparison.OrdinalIgnoreCase)
            && engine.Lifetime.Clicks == 0 && engine.Insights.CurrentStreak == 0,
            "new-user funny facts do not invent activity or imply a streak");
    }
    foreach (var invalidState in new[] { (Name: "missing-version", Json: "{}"),
        (Name: "null-rewards", Json: "{\"Version\":1,\"ChallengeRewards\":null}") })
    {
        using var store = new Store(Profile(invalidState.Name));
        store.Save("2026-11-05", new() { LeftClicks = 9 }, new() { LeftClicks = 9 }, ["click100"], false, true, false, 96, invalidState.Json);
        bool rejected = false;
        try { _ = new ActivityEngine(store, () => new DateTime(2026, 11, 5), () => seconds); }
        catch (IOException) { rejected = true; }
        Check(rejected && store.ReadExpansionState() == invalidState.Json && store.ReadTotals("lifetime").Clicks == 9
            && store.ReadAchievements().Contains("click100"),
            "invalid expansion state " + invalidState.Name + " fails without silently replacing rewards or activity history");
    }
    using (var store = new Store(challengeProfile))
    {
        var engine = new ActivityEngine(store, () => local, () => seconds);
        string[] facts = Enumerable.Range(0, 32).Select(engine.Insights.GetFunnyFact).Distinct().ToArray();
        Check(facts.Any(f => f.Contains(engine.Lifetime.Clicks.ToString("N0")) && f.Contains("recorded mouse clicks"))
            && facts.Any(f => f.Contains("Estimated comparison:") && f.Contains("100-meter")),
            "rotating facts use real totals and explicitly label defined distance comparisons as estimates");
        Check(!string.IsNullOrWhiteSpace(engine.Insights.GetFunnyFact(int.MinValue)),
            "fact rotation remains bounded and valid for wrapped rotation indices");
    }
    Console.WriteLine($"{results.Count} independent lifecycle checks passed. Profiles: {fixtures}");
    File.WriteAllLines(Path.Combine(evidence, "results.txt"), results.Append($"{results.Count} checks passed.").Append("Disposable profiles: " + fixtures));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    File.WriteAllLines(Path.Combine(evidence, "results.txt"), results.Append(ex.ToString()).Append("Disposable profiles: " + fixtures));
    return 1;
}
