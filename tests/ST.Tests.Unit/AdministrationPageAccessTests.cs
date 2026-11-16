using ST.SharedKernel.Features.IAM.Menus;

namespace ST.Tests.Unit;

public sealed class AdministrationPageAccessTests
{
    [Theory]
    [InlineData("/admin/menus", "users.view", false)]
    [InlineData("/admin/access-catalog", "roles.view", false)]
    [InlineData("/admin/menus", "menus.view", true)]
    [InlineData("/admin/org-settings", "tenant-settings.view", true)]
    [InlineData("/admin/org-settings", "users.view", false)]
    [InlineData("/admin/iam", "users.view", true)]
    [InlineData("/admin/iam", "roles.view", true)]
    [InlineData("/admin/iam", "roles.manage_permissions", true)]
    [InlineData("/admin/iam", "users.edit", false)]
    [InlineData("/admin", "control_plane.view_kyc", false)]
    [InlineData("/admin/support", "support.approve", true)]
    public void ShippedPagesRequireAnImplementedReadOperation(string path, string permission, bool allowed)
    {
        Assert.Equal(allowed, AdministrationPageAccess.CanOpen(path, [permission]));
    }
}
