using System.ComponentModel.DataAnnotations;

namespace ST.SharedKernel.Features.IAM.Users.Dtos;

public sealed record CreateRoleRequest([Required, StringLength(80)] string Name, Guid? DepartmentId = null);
