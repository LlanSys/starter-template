namespace ST.SharedKernel.Features.ControlPlane.Modules.Dtos;
public sealed record ApplyRoleTemplateRequest(string TemplateKey, string RoleName, int ExpectedVersion);
