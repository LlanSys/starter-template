using ST.Application.Contracts.Interfaces.Common;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.SharedKernel.Dtos.Common;
using ST.SharedKernel.Features.Shared.Dashboard.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using ST.SharedKernel.Extensions;

namespace ST.Application.Features.Shared.Dashboard.QueryHandlers;




internal sealed class GetDashboardSummaryQueryHandler(IBankingUnitOfWork _bankingUnitOfWork, ILogger<GetDashboardSummaryQueryHandler> _logger)
    : IRequestHandler<GetDashboardSummaryQuery, AppResponse<DashboardSummaryResponse>>
{
    // Standard capacity per RM — configurable via IAM settings in a future iteration
    private const int MaxCapacity = 40;

    public async Task<AppResponse<DashboardSummaryResponse>> Handle(GetDashboardSummaryQuery query, CancellationToken ct)
    {

        try
        {
            var now = DateTimeOffset.UtcNow;

            var rows = await _bankingUnitOfWork.CustomerRepository
                .ListAsync(
                    customers => customers.Select(c => new Row(
                        c.Status,
                        c.SegmentType,
                        c.Type,
                        c.OpenedOn,
                        c.RelationshipManager != null
                            ? $"{c.RelationshipManager.FirstName} {c.RelationshipManager.LastName}"
                            : null)),
                    ct)
                .ConfigureAwait(false);

            // ── KPIs ───────────────────────────────────────────────────────────
            var total = rows.Count;
            var active = rows.Count(c => c.Status == CustomerStatus.Active);
            var pendingApproval = rows.Count(c => c.Status == CustomerStatus.PendingApproval);
            var draft = rows.Count(c => c.Status == CustomerStatus.Draft);

            // ── Breakdowns ─────────────────────────────────────────────────────
            var bySegment = Breakdown(rows, r => r.SegmentType.ToDisplayString(), r => r.Status);
            var byCustomerType = Breakdown(rows, r => r.Type.ToDisplayString(), r => r.Status);

            // ── Aging ──────────────────────────────────────────────────────────
            var pendingRows = rows.Where(r => r.Status == CustomerStatus.PendingApproval).ToList();
            var draftRows = rows.Where(r => r.Status == CustomerStatus.Draft).ToList();

            var aging = new AgingData(
                BuildAgingBucket(pendingRows, now),
                BuildAgingBucket(draftRows, now));

            // ── RM workload table ──────────────────────────────────────────────
            var rmWorkload = rows
                .Where(r => r.RmName is not null)
                .GroupBy(r => r.RmName!)
                .Select(g =>
                {
                    var rmTotal = g.Count();
                    var rmActive = g.Count(r => r.Status == CustomerStatus.Active);
                    var rmPending = g.Count(r => r.Status == CustomerStatus.PendingApproval);
                    var rmDraft = g.Count(r => r.Status == CustomerStatus.Draft);
                    var capacity = Math.Min((int)Math.Round(rmTotal * 100.0 / MaxCapacity), 150);

                    return new RmWorkloadRow(
                        g.Key,
                        BuildInitials(g.Key),
                        rmTotal,
                        rmActive,
                        rmPending,
                        rmDraft,
                        capacity);
                })
                .OrderByDescending(r => r.Total)
                .ToList();

            return AppResponses.Success<DashboardSummaryResponse>(new(
                total, active, pendingApproval, draft,
                bySegment, byCustomerType, aging, rmWorkload));
        }
        catch (Exception ex)
        {
            LogDefinitions.LogDashboardSummaryFetchFailed(_logger, ex);
            throw;
        }
    }



    /// <summary>
    /// Groups a row set by a label, builds per-status counts for each group.
    /// Works for any dimension: segment, customer type, or sub-breakdowns within an RM.
    /// </summary>
    private static IReadOnlyList<BreakdownGroup> Breakdown<T>(
        IEnumerable<T> source, Func<T, string> labelSelector, Func<T, CustomerStatus> statusSelector)
    {
        return [.. source
                .GroupBy(labelSelector)
                .Select(g =>
                {
                    var counts = g.GroupBy(statusSelector)
                                  .ToDictionary(x => x.Key, x => x.Count());

                    return new BreakdownGroup(
                        g.Key,
                        g.Count(),
                        StatusMeta
                            .Select(s => new StatusCount(
                                s.Status.ToDisplayString(),
                                counts.GetValueOrDefault(s.Status, 0),
                                s.Color))
                            .Where(s => s.Count > 0)
                            .ToList());
                })
                .OrderByDescending(g => g.Total)];
    }

    private static string BuildInitials(string fullName)
    {
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
            : fullName[..Math.Min(2, fullName.Length)].ToUpperInvariant();
    }

    private static AgingBucket BuildAgingBucket(IReadOnlyList<Row> rows, DateTimeOffset now)
    {
        if (!rows.Any())
        {
            return new AgingBucket(0, 0, 0, 0, 0, 0);
        }

        var ages = rows.Select(r => (now - r.OpenedOn).TotalDays).ToList();

        return new AgingBucket(
            Total: rows.Count,
            AvgDays: Math.Round(ages.Average(), 1),
            Over14Days: ages.Count(d => d > 14),
            Days7To14: ages.Count(d => d >= 7 && d <= 14),
            Days3To6: ages.Count(d => d >= 3 && d < 7),
            Under3Days: ages.Count(d => d < 3));
    }

    // A concrete projected row — avoids anonymous type inference issues
    private sealed record Row(
        CustomerStatus Status,
        SegmentType SegmentType,
        CustomerType Type,
        DateTimeOffset OpenedOn,
        string? RmName);

    // Status display order and colors — single definition used everywhere
    private static readonly (CustomerStatus Status, string Color)[] StatusMeta =
    [
        (CustomerStatus.Active,          "#1D9E75"),
        (CustomerStatus.PendingApproval, "#378ADD"),
        (CustomerStatus.Draft,           "#EF9F27"),
        (CustomerStatus.Suspended,       "#E24B4A"),
        (CustomerStatus.Closed,          "#888780")
    ];

}
