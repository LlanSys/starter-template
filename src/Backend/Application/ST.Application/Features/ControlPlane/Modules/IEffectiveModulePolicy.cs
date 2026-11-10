namespace ST.Application.Features.ControlPlane.Modules;
public interface IEffectiveModulePolicy
{
    Task<EffectiveModuleAccess> GetAsync(CancellationToken cancellationToken = default);
}
