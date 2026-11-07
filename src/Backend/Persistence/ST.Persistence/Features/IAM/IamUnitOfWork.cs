using ST.Persistence.Features.IAM.Users.Repositories;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.IAM.Menus.Contracts.Repositories;
using ST.Domain.Features.IAM.Permissions.Contracts.Repositories;
using ST.Domain.Features.IAM.ReferenceData.Entities;
using ST.Domain.Features.IAM.Users.Contracts.Repositories;
using ST.Domain.Shared.Contracts.Repositories;
using ST.Persistence.Common;
using ST.Persistence.Common.Repositories;
using ST.Persistence.Features.IAM.DataContext;
using MediatR;
using Microsoft.Extensions.Logging;
using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Features.IAM.Users.Entities;

namespace ST.Persistence.Features.IAM;

public sealed class IamUnitOfWork(
    IamDBContext context,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    ITokenRepository tokenRepository,
    IAppUserProfileRepository appUserProfileRepository,
    IAppUserTotpSecretRepository appUserTotpSecretRepository,
    ITempTotpSecretRepository tempTotpSecretRepository,
    IPermissionRepository permissionRepository,
    IMenuRepository menuRepository,
    IPublisher publisher,
    ILogger<IamUnitOfWork> logger
) : BaseUnitOfWork<IamDBContext>(context, publisher, logger, context.Clock), IIamUnitOfWork
{
    public IRepository<AppRole> RoleRepository { get; } = new Repository<AppRole>(context);
    public IRepository<NavigationOverride> NavigationOverrides { get; } = new Repository<NavigationOverride>(context);
    public IUserRepository UserRepository { get; } = userRepository;
    public ISessionRepository SessionRepository { get; } = sessionRepository;
    public ITokenRepository TokenRepository { get; } = tokenRepository;
    public IAppUserProfileRepository AppUserProfileRepository { get; } = appUserProfileRepository;
    public IAppUserTotpSecretRepository AppUserTotpSecretRepository { get; } = appUserTotpSecretRepository;
    public ITempTotpSecretRepository TempTotpSecretRepository { get; } = tempTotpSecretRepository;
    public IPermissionRepository PermissionRepository { get; } = permissionRepository;
    public IMenuRepository MenuRepository { get; } = menuRepository;
    public IRepository<PermissionContext> PermissionContextRepository { get; } = new Repository<PermissionContext>(context);
    public IRepository<PermissionResource> PermissionResourceRepository { get; } = new Repository<PermissionResource>(context);
    public IRepository<PermissionAction> PermissionActionRepository { get; } = new Repository<PermissionAction>(context);
    public IRepository<MenuPlacement> MenuPlacementRepository { get; } = new Repository<MenuPlacement>(context);
    public IRepository<MenuIcon> MenuIconRepository { get; } = new Repository<MenuIcon>(context);
    public IRepository<MenuRoute> MenuRouteRepository { get; } = new Repository<MenuRoute>(context);
    public IPasskeyChallengeRepository PasskeyChallengeRepository { get; } = new IamPasskeyChallengeRepository(context);
    public IRepository<Fido2Credential> Fido2CredentialRepository { get; } = new Repository<Fido2Credential>(context);
}
