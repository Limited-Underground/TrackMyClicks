// Copyright 2026 Nenad Bjelanovic
// SPDX-License-Identifier: Apache-2.0
using System.Windows;
using System.Windows.Input;

namespace TrackMyClicks;

public partial class AchievementShowcase : Window
{
    private IEnumerable<ExpansionCard>? challengeSource;
    public AchievementShowcase(IEnumerable<ExpansionCard> badges, IEnumerable<ExpansionCard> challenges, string summary)
    {
        InitializeComponent();
        CategoryGroups.ItemsSource = badges.GroupBy(b => b.Group)
            .Select(g => new ShowcaseCategory(g.Key, g.ToArray())).ToArray();
        RefreshChallenges(challenges); UpdateSummary(summary);
    }

    public void UpdateSummary(string summary)
    {
        if (SummaryText.Text != summary) SummaryText.Text = summary;
    }

    public void RefreshChallenges(IEnumerable<ExpansionCard> challenges)
    {
        if (ReferenceEquals(challengeSource, challenges)) return;
        challengeSource = challenges; ChallengeItems.ItemsSource = challenges;
    }

    private void BackClick(object sender, RoutedEventArgs e) { var owner = Owner; Close(); owner?.Activate(); }
    private void WindowKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        e.Handled = true; var owner = Owner; Close(); owner?.Activate();
    }
}

public sealed record ShowcaseCategory(string Title, IReadOnlyList<ExpansionCard> Cards);
