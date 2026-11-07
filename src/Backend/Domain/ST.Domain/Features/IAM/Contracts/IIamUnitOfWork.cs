using ST.Domain.Shared.Contracts;
using ST.Domain.Features.IAM.Menus.Contracts.Repositories;
using ST.Domain.Features.IAM.Permissions.Contracts.Repositories;
using ST.Domain.Features.IAM.ReferenceData.Entities;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;

namespace ST.Domain.Features.IAM.Contracts;

public interface IIamUnitOfWork : ITransactionalUnitOfWork
{
    IRepository<ST.Domain.Features.IAM.Users.Entities.AppRole> RoleRepository { get; }
    IRepository<ST.Domain.Features.IAM.Menus.Entities.NavigationOverride> NavigationOverrides { get; }
    IUserRepository UserRepository { get; }
    ISessionRepository SessionRepository { get; }
    ITokenRepository TokenRepository { get; }
    IAppUserProfileRepository AppUserProfileRepository { get; }
    IAppUserTotpSecretRepository AppUserTotpSecretRepository { get; }
    ITempTotpSecretRepository TempTotpSecretRepository { get; }
    IPermissionRepository PermissionRepository { get; }
    IMenuRepository MenuRepository { get; }
    IRepository<PermissionContext> PermissionContextRepository { get; }
    IRepository<PermissionResource> PermissionResourceRepository { get; }
    IRepository<PermissionAction> PermissionActionRepository { get; }
    IRepository<MenuPlacement> MenuPlacementRepository { get; }
    IRepository<MenuIcon> MenuIconRepository { get; }
    IRepository<MenuRoute> MenuRouteRepository { get; }
    IPasskeyChallengeRepository PasskeyChallengeRepository { get; }
    IRepository<Fido2Credential> Fido2CredentialRepository { get; }
}
