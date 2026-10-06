namespace ST.Persistence.Common;

internal interface ITenantFilteredDBContext
{
    Guid CurrentTenantId { get; }
}
