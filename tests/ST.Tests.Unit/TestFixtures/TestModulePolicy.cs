using ST.Application.Features.ControlPlane.Modules;
namespace ST.Tests.Unit.TestFixtures;
internal sealed class TestModulePolicy : IEffectiveModulePolicy
{
    public EffectiveModuleAccess Access { get; set; } = new() { Modules = new HashSet<string>(["core", "iam", "payments", "showcase"], StringComparer.OrdinalIgnoreCase), Catalogue = [], EnabledFlags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) };
    public Task<EffectiveModuleAccess> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Access);
}
