namespace ST.Domain.Shared.Contracts.Common;

public interface ICurrentActorProvider
{
    public const string SystemActor = "System";

    string ActorId { get; }
}
