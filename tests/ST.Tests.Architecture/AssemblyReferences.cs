using System.Reflection;
using ST.Api.Common.Controllers;
using ST.Application.Features.Shared.OrgSettings.SaveCatalogSetting;
using ST.Domain.Shared.Entities;
using ST.Persistence.Features.Shared.DataContext;
using ST.SharedKernel.Features.Shared.Lookups.Dtos;
using ST.SharedKernel.Validation.Features.Shared.Lookups.Validators;
using ST.UI.Blazor.Components;
using InfrastructureRegistration = ST.Infrastructure.Extensions.DependencyInjection;

namespace ST.Tests.Architecture;

internal static class AssemblyReferences
{
    internal static readonly Assembly Domain = typeof(BaseEntity).Assembly;
    internal static readonly Assembly Api = typeof(BaseController).Assembly;
    internal static readonly Assembly Application = typeof(SaveCatalogOrgSettingCommand).Assembly;
    internal static readonly Assembly Infrastructure = typeof(InfrastructureRegistration).Assembly;
    internal static readonly Assembly Persistence = typeof(SharedDBContext).Assembly;
    internal static readonly Assembly SharedKernel = typeof(LookupResponse).Assembly;
    internal static readonly Assembly SharedKernelValidation = typeof(GetLookupRequestValidator).Assembly;
    internal static readonly Assembly Blazor = typeof(App).Assembly;

    internal static string RepoRoot => FindRepoRoot();

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "src")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate repository root from architecture test output directory.");
    }
}
