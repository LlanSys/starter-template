using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Features.ControlPlane.Modules.Entities;
using ST.Application.Features.IAM.Menus.Mappings;
using ST.SharedKernel.Features.IAM.Menus.Dtos;
namespace ST.Application.Features.IAM.Menus;
public sealed record ResolvedNavigationItem(MenuItem Menu, ModuleCatalogueEntry? Definition, bool Available, bool IsOverride, int OverrideVersion)
{
    public MenuResponse ToResponse() => Menu.ToMenuResponse() with { CatalogueKey = Definition?.Key, IsOverride = IsOverride, DefinitionVersion = Definition?.Version ?? 0, OverrideVersion = OverrideVersion };
}
