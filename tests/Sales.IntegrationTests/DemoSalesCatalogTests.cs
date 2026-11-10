using Microsoft.Extensions.Configuration;
using SmartSales.Sales.Domain.Entities;
using SmartSales.Sales.Infrastructure.Persistence;

namespace SmartSales.Sales.IntegrationTests;

public sealed class DemoSalesCatalogTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-09T12:00:00Z");
    private static readonly DateOnly Today = new(2026, 11, 9);

    [Fact]
    public void Basic_opportunities_catalog_has_exact_eight_original_rows()
    {
        var opps = DemoSalesSeeder.GenerateOpportunities(isShowcase: false, Now, Today);
        Assert.Equal(8, opps.Length);
        Assert.Equal("Primera conversación", opps[0].Title);
        Assert.Equal("Expansión regional", opps[7].Title);

        Assert.Equal(2, opps.Count(o => o.Stage == "New"));
        Assert.Equal(2, opps.Count(o => o.Stage == "Contacted"));
        Assert.Equal(2, opps.Count(o => o.Stage == "Proposal"));
        Assert.Equal(1, opps.Count(o => o.Stage == "Won"));
        Assert.Equal(1, opps.Count(o => o.Stage == "Lost"));
    }

    [Fact]
    public void Showcase_opportunities_catalog_has_exact_one_hundred_rows_and_exact_stage_distribution()
    {
        var opps = DemoSalesSeeder.GenerateOpportunities(isShowcase: true, Now, Today);
        Assert.Equal(100, opps.Length);
        Assert.Equal(100, opps.Select(o => o.Id).Distinct().Count());

        // Exact stage distribution required by GOAL 01.2:
        // New (25), Contacted (25), Proposal (25), Won (15), Lost (10)
        Assert.Equal(25, opps.Count(o => o.Stage == "New"));
        Assert.Equal(25, opps.Count(o => o.Stage == "Contacted"));
        Assert.Equal(25, opps.Count(o => o.Stage == "Proposal"));
        Assert.Equal(15, opps.Count(o => o.Stage == "Won"));
        Assert.Equal(10, opps.Count(o => o.Stage == "Lost"));

        // Check constraints validation
        Assert.All(opps, o =>
        {
            Assert.True(o.EstimatedAmount >= 0);
            Assert.Contains(o.Priority, new[] { "High", "Medium", "Low" });
            Assert.Contains(o.OwnerUserId, DemoSalesSeeder.ShowcaseOwnerIds);
            Assert.Contains(o.ContactId, DemoSalesSeeder.ContactIds);

            if (o.Stage == "Won")
            {
                Assert.NotNull(o.ClosedAt);
                Assert.NotNull(o.ClosedByUserId);
                Assert.NotNull(o.FinalAmount);
                Assert.True(o.FinalAmount >= 0);
                Assert.Null(o.LossReason);
            }
            else if (o.Stage == "Lost")
            {
                Assert.NotNull(o.ClosedAt);
                Assert.NotNull(o.ClosedByUserId);
                Assert.Null(o.FinalAmount);
                Assert.False(string.IsNullOrWhiteSpace(o.LossReason));
            }
            else
            {
                Assert.Null(o.ClosedAt);
                Assert.Null(o.ClosedByUserId);
                Assert.Null(o.FinalAmount);
                Assert.Null(o.LossReason);
            }
        });
    }

    [Fact]
    public void Showcase_follow_ups_catalog_has_exact_one_hundred_twenty_rows_and_valid_fks()
    {
        var opps = DemoSalesSeeder.GenerateOpportunities(isShowcase: true, Now, Today);
        var oppMap = opps.ToDictionary(o => o.Id);
        var followUps = DemoSalesSeeder.GenerateFollowUps(isShowcase: true, Now, Today, oppMap);

        Assert.Equal(120, followUps.Length);
        Assert.Equal(120, followUps.Select(f => f.Id).Distinct().Count());

        // Check categories at seed time: exactly 30 Overdue, 30 Today, 30 Upcoming, 30 Completed
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(Now, DemoSalesSeeder.Bogota).DateTime);
        var tomorrowUtc = DemoSalesSeeder.BogotaMidnightUtc(localDate.AddDays(1));

        var overdue = followUps.Count(f => f.Status == "Pending" && f.ScheduledAt < Now);
        var today = followUps.Count(f => f.Status == "Pending" && f.ScheduledAt >= Now && f.ScheduledAt < tomorrowUtc);
        var upcoming = followUps.Count(f => f.Status == "Pending" && f.ScheduledAt >= tomorrowUtc);
        var completed = followUps.Count(f => f.Status == "Completed");

        Assert.Equal(30, overdue);
        Assert.Equal(30, today);
        Assert.Equal(30, upcoming);
        Assert.Equal(30, completed);

        // Check composite foreign key rule: OpportunityId and ContactId must match
        Assert.All(followUps, f =>
        {
            if (f.OpportunityId is { } oppId)
            {
                Assert.True(oppMap.ContainsKey(oppId));
                Assert.Equal(oppMap[oppId].ContactId, f.ContactId);

                // Pending follow-ups must not be linked to closed opportunities
                if (f.Status == "Pending")
                {
                    Assert.Contains(oppMap[oppId].Stage, new[] { "New", "Contacted", "Proposal" });
                }
            }

            if (f.Status == "Completed")
            {
                Assert.NotNull(f.CompletedAt);
                Assert.NotNull(f.CompletedByUserId);
            }
            else
            {
                Assert.Null(f.CompletedAt);
                Assert.Null(f.CompletedByUserId);
            }
        });
    }

    [Fact]
    public void Follow_ups_distribution_today_holds_even_late_at_night()
    {
        // 23:55:00 Bogota time (UTC-5 -> 04:55:00Z next day)
        var lateNight = DateTimeOffset.Parse("2026-11-10T04:55:00Z");
        var opps = DemoSalesSeeder.GenerateOpportunities(isShowcase: true, lateNight, Today);
        var oppMap = opps.ToDictionary(o => o.Id);
        var followUps = DemoSalesSeeder.GenerateFollowUps(isShowcase: true, lateNight, Today, oppMap);

        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(lateNight, DemoSalesSeeder.Bogota).DateTime);
        var tomorrowUtc = DemoSalesSeeder.BogotaMidnightUtc(localDate.AddDays(1));

        var todayGroup = followUps.Where(f => f.Status == "Pending" && f.ScheduledAt >= lateNight && f.ScheduledAt < tomorrowUtc).ToArray();
        Assert.Equal(30, todayGroup.Length);
    }

    [Fact]
    public void Pending_follow_ups_do_not_attach_to_closed_opportunities_after_prior_closure()
    {
        var opps = DemoSalesSeeder.GenerateOpportunities(isShowcase: true, Now, Today);
        // Simulate that user closed several open opportunities during demo
        opps[0].Stage = "Won";
        opps[0].ClosedAt = Now;
        opps[1].Stage = "Lost";
        opps[1].ClosedAt = Now;

        var oppMap = opps.ToDictionary(o => o.Id);
        var followUps = DemoSalesSeeder.GenerateFollowUps(isShowcase: true, Now, Today, oppMap);

        Assert.All(followUps.Where(f => f.Status == "Pending"), f =>
        {
            if (f.OpportunityId is { } oppId)
            {
                Assert.NotEqual(opps[0].Id, oppId);
                Assert.NotEqual(opps[1].Id, oppId);
                Assert.Contains(oppMap[oppId].Stage, new[] { "New", "Contacted", "Proposal" });
            }
        });
    }

    [Fact]
    public void Showcase_interactions_catalog_has_exact_one_hundred_fifty_rows_and_valid_types()
    {
        var opps = DemoSalesSeeder.GenerateOpportunities(isShowcase: true, Now, Today);
        var oppMap = opps.ToDictionary(o => o.Id);
        var interactions = DemoSalesSeeder.GenerateInteractions(isShowcase: true, Now, oppMap);

        Assert.Equal(150, interactions.Length);
        Assert.Equal(150, interactions.Select(i => i.Id).Distinct().Count());

        // Check type distribution: 50 Call, 50 Email, 50 Meeting
        Assert.Equal(50, interactions.Count(i => i.Type == "Call"));
        Assert.Equal(50, interactions.Count(i => i.Type == "Email"));
        Assert.Equal(50, interactions.Count(i => i.Type == "Meeting"));

        // Check that composite FK matches and OccurredAt is past
        Assert.All(interactions, i =>
        {
            Assert.True(i.OccurredAt <= Now);
            if (i.OpportunityId is { } oppId)
            {
                Assert.True(oppMap.ContainsKey(oppId));
                Assert.Equal(oppMap[oppId].ContactId, i.ContactId);
            }
        });
    }

    [Fact]
    public void Profile_validator_rejects_invalid_profiles_or_disabled_flag()
    {
        var configDisabled = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Demo:Enabled"] = "false" }).Build();

        Assert.Throws<InvalidOperationException>(() =>
            DemoSeedProfile.IsShowcase(configDisabled, null));

        var configInvalid = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Demo:Enabled"] = "true",
                ["Demo:Profile"] = "InvalidProfile"
            }).Build();

        Assert.Throws<InvalidOperationException>(() =>
            DemoSeedProfile.IsShowcase(configInvalid, null));
    }
}
