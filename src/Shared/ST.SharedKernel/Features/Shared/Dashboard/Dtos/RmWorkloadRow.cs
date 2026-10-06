using System;
using System.Collections.Generic;
using System.Text;

namespace ST.SharedKernel.Features.Shared.Dashboard.Dtos;

/// <summary>
/// Flat table row for RM workload — one row per RM showing all status counts
/// and a capacity percentage. Capacity is relative to MaxCapacity (default 40).
/// </summary>
public record RmWorkloadRow(
    string RmName,
    string Initials,
    int Total,
    int Active,
    int PendingApproval,
    int Draft,
    int CapacityPercentage);  // Total / MaxCapacity * 100, capped at 150