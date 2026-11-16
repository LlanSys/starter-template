namespace ST.Application.Features.Shared.Dashboard;

public sealed record DashboardAttentionAccess(
    bool Kyc,
    bool Provisioning,
    bool Invitations,
    bool LegalHolds,
    bool SupportApprovals,
    bool ApprovedSupport);
