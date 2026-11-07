using ST.Domain.Features.ControlPlane.Modules.Entities;
namespace ST.Tests.Unit;
public sealed class ModuleLifecycleTests
{
    [Theory]
    [InlineData("Core")]
    [InlineData("iam")]
    public void EssentialModulesCannotBeDisabledOrRetired(string key)
    {
        var module = new ModuleDefinition { Key = key, Name = key, CreatedBy = "vendor" };
        Assert.False(module.ChangeAvailability(false, false, "vendor"));
        Assert.False(module.ChangeAvailability(false, true, "vendor"));
        Assert.True(module.IsActive);
        Assert.False(module.IsRetired);
    }
    [Fact]
    public void DisableIsReversibleButRetirementPreservesIdentityAndIsTerminal()
    {
        var module = new ModuleDefinition { Key = "Payments", Name = "Payments", CreatedBy = "vendor" };
        var id = module.Id;
        Assert.True(module.ChangeAvailability(false, false, "vendor"));
        Assert.False(module.IsActive);
        Assert.True(module.ChangeAvailability(true, false, "vendor"));
        Assert.True(module.IsActive);
        Assert.True(module.ChangeAvailability(false, true, "vendor"));
        Assert.False(module.ChangeAvailability(true, false, "vendor"));
        Assert.True(module.IsRetired);
        Assert.False(module.IsActive);
        Assert.Equal(id, module.Id);
        Assert.Equal("vendor", module.UpdatedBy);
    }
}
