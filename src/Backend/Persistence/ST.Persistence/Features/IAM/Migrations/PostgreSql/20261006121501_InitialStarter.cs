using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.IAM.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class InitialStarter : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MenuIcons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0]),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuIcons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Icon = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Placement = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    RequiredPermissionKey = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    RequiredModule = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_MenuItems_ParentId",
                        column: x => x.ParentId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MenuPlacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0]),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuPlacements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuRoutes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Url = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    PlacementKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0]),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasskeyChallenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", unicode: false, maxLength: 128, nullable: false),
                    OptionsJson = table.Column<string>(type: "text", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasskeyChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0]),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0]),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionContexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionResources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ContextKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0]),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Context = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Resource = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "text", nullable: true),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastFailedLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RequirePasswordChange = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordLastChanged = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TotpSecret = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeactivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeactivatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DeactivationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0]),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleClaims_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsTrusted = table.Column<bool>(type: "boolean", nullable: false),
                    TrustedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserDevices_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TelephoneNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    MobileNo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserProfiles_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    LastAccessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DeviceFingerprint = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserSessions_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppUserTotpSecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EncryptedSecret = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserTotpSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppUserTotpSecrets_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fido2Credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    UserHandle = table.Column<byte[]>(type: "bytea", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "bytea", nullable: false),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    CredType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RegDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AaGuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fido2Credentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fido2Credentials_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Token = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RevokedByIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TokenFamily = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TempTotpSecrets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    EncryptedSecret = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: false, defaultValue: new byte[0])
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TempTotpSecrets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TempTotpSecrets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserClaims_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_UserLogins_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_UserTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "MenuIcons",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10501"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "AccountTree", "Account tree", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10502"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "AdminPanelSettings", "Admin panel", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10503"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "AutoStories", "Story/book", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10504"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "Badge", "Badge", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10505"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "Business", "Business", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10506"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "Dashboard", "Dashboard", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10507"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "Devices", "Devices", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10508"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "Group", "Group", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10509"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "LockPerson", "Security lock", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10510"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "Menu", "Generic menu", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10511"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "MenuOpen", "Menu", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10512"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "Settings", "Settings", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10513"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "CreditCard", "Credit card", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10514"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved MudBlazor icon key.", true, false, "Dns", "DNS/platform", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "Icon", "IsActive", "IsDeleted", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "RowVersion", "TenantId", "Title", "UpdatedAt", "UpdatedBy", "Url" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10101"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Operations dashboard.", 2, "Dashboard", true, false, "dashboard", null, "Sidebar", "core", null, new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Dashboard", null, null, "/dashboard" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Administrative workspace.", 3, "AdminPanelSettings", true, false, "admin-center", null, "Sidebar", "core", null, new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Admin Center", null, null, "/admin" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10301"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Architecture and solution overview.", 1, "AutoStories", true, false, "solution-overview", null, "Sidebar", "showcase", null, new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Solution Overview", null, null, "/overview" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Reusable platform capability showcases.", 4, "MenuOpen", true, false, "features", null, "Sidebar", "showcase", null, new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Features", null, null, "/features" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Platform management.", 4, "Dns", true, false, "control-panel", null, "Sidebar", null, "control_plane.manage", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Control Panel", null, null, "/control-panel" }
                });

            migrationBuilder.InsertData(
                table: "MenuPlacements",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10401"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Main application navigation.", true, false, "Sidebar", "Sidebar", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10402"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Administration landing tiles.", true, false, "AdminCenter", "Admin Center", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10403"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Platform control plane tiles.", true, false, "ControlPanel", "Control Panel", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuRoutes",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "PlacementKey", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy", "Url" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10601"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "dashboard", "Dashboard", "Sidebar", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/dashboard" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10602"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-center", "Admin Center", "Sidebar", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10603"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "solution-overview", "Solution Overview", "Sidebar", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/overview" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10607"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-menus", "Menus", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/menus" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10608"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-permissions", "Permissions", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/permissions" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10609"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-roles", "Roles", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/roles" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10610"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-settings", "Settings", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/settings" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10611"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-user-devices", "User Devices", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/user-devices" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10612"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-users", "Users", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/users" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10613"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-access-catalog", "Access Catalog", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/access-catalog" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10614"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "features", "Features", "Sidebar", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/features" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10615"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "features-payments", "Payments", "Sidebar", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/features/payments" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10616"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "control-panel", "Control Panel", "Sidebar", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/control-panel" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10617"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "control-panel-tenants", "Tenants", "ControlPanel", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/control-panel/tenants" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10618"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "control-panel-stamps", "Stamps", "ControlPanel", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/control-panel/stamps" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10619"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-org-settings", "Org Settings", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/org-settings" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10620"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Approved application route.", true, false, "admin-iam", "Identity & Access", "AdminCenter", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null, "/admin/iam" }
                });

            migrationBuilder.InsertData(
                table: "PermissionActions",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10301"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Read and list records.", true, false, "view", "View", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10302"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Create new records.", true, false, "create", "Create", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10303"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Update existing records.", true, false, "edit", "Edit", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10304"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Soft-delete or remove records.", true, false, "delete", "Delete", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10305"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Disable active records or accounts.", true, false, "deactivate", "Deactivate", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10306"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Assign or revoke permissions.", true, false, "manage_permissions", "Manage permissions", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10307"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Assign or revoke roles.", true, false, "manage_roles", "Manage roles", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10308"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Perform restricted provider administration actions.", true, false, "admin", "Administer", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10309"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage a platform capability.", true, false, "manage", "Manage", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "PermissionContexts",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10103"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Users, roles, permissions, sessions, and devices.", true, false, "IAM", "Identity and Access", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10104"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Cross-cutting platform configuration and navigation.", true, false, "Platform", "Platform", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10105"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Cross-cutting platform services and reusable capability showcases.", true, false, "Shared", "Shared", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10106"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Tenant approval and deployment administration.", true, false, "ControlPlane", "Control Plane", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "PermissionResources",
                columns: new[] { "Id", "ContextKey", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Label", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10204"), "Platform", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Navigation registry and menu visibility.", true, false, "menus", "Menus", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10205"), "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Permission catalog and assignment surface.", true, false, "permissions", "Permissions", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10206"), "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Role catalog and permission bundles.", true, false, "roles", "Roles", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10207"), "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Application user accounts.", true, false, "users", "Users", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10208"), "Shared", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Payment checkout, status, and provider administration.", true, false, "payments", "Payments", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9d10209"), "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Tenant and stamp administration.", true, false, "control_plane", "Control Plane", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "Context", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Key", "Resource", "RowVersion", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10101"), "view", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "View application users.", true, false, "users.view", "users", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10102"), "create", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Create application users.", true, false, "users.create", "users", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10103"), "edit", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Update application users.", true, false, "users.edit", "users", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10104"), "deactivate", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Deactivate application users.", true, false, "users.deactivate", "users", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10105"), "manage_roles", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage user role assignments.", true, false, "users.manage_roles", "users", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10106"), "manage_permissions", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage direct user permissions.", true, false, "users.manage_permissions", "users", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10201"), "view", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "View platform roles.", true, false, "roles.view", "roles", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10202"), "create", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Create platform roles.", true, false, "roles.create", "roles", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10203"), "edit", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Update platform roles.", true, false, "roles.edit", "roles", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10204"), "delete", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Delete platform roles.", true, false, "roles.delete", "roles", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10205"), "manage_permissions", "IAM", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage role permission assignments.", true, false, "roles.manage_permissions", "roles", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10601"), "view", "Platform", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "View menu catalog.", true, false, "menus.view", "menus", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10602"), "create", "Platform", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Create menu items.", true, false, "menus.create", "menus", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10603"), "edit", "Platform", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Update menu items.", true, false, "menus.edit", "menus", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10604"), "delete", "Platform", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Delete menu items.", true, false, "menus.delete", "menus", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10701"), "view", "Shared", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "View payment history and provider readiness.", true, false, "payments.view", "payments", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10702"), "create", "Shared", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Initiate payment checkout flows.", true, false, "payments.create", "payments", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10703"), "admin", "Shared", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage M-Pesa payment administration actions.", true, false, "payments.admin", "payments", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9b10801"), "manage", "ControlPlane", new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage Control Plane Tenants and Stamps.", true, false, "control_plane.manage", "control_plane", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "MenuItems",
                columns: new[] { "Id", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "DisplayOrder", "Icon", "IsActive", "IsDeleted", "Key", "ParentId", "Placement", "RequiredModule", "RequiredPermissionKey", "RowVersion", "TenantId", "Title", "UpdatedAt", "UpdatedBy", "Url" },
                values: new object[,]
                {
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10402"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Test card and mobile-money payment flows.", 10, "CreditCard", true, false, "features-payments", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10401"), "Features", "showcase", "payments.view", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Payments", null, null, "/features/payments" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20104"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Navigation catalog and menu visibility.", 40, "MenuOpen", true, false, "admin-menus", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "core", null, new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Menus", null, null, "/admin/menus" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20107"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Tenant-specific configuration surface.", 50, "Settings", true, false, "admin-org-settings", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "core", null, new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Org Settings", null, null, "/admin/org-settings" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20110"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Source-of-truth permission and menu reference data.", 70, "LockPerson", true, false, "admin-access-catalog", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "iam", null, new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Access Catalog", null, null, "/admin/access-catalog" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c20111"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage users, roles, permissions, and trusted devices.", 60, "Group", true, false, "admin-iam", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10201"), "AdminCenter", "iam", null, new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Identity & Access", null, null, "/admin/iam" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30101"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Manage SaaS tenants.", 10, "Business", true, false, "control-panel-tenants", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"), "ControlPanel", null, "control_plane.manage", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Tenants", null, null, "/control-panel/tenants" },
                    { new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c30102"), new DateTimeOffset(new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Deployment stamps.", 20, "Dns", true, false, "control-panel-stamps", new Guid("018fd81d-2c94-7ad0-a4a3-f1edb9c10501"), "ControlPanel", null, "control_plane.manage", new byte[0], new Guid("0194f700-0000-7000-8000-000000000001"), "Stamps", null, null, "/control-panel/stamps" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserDevices_AppUserId_DeviceFingerprint",
                table: "AppUserDevices",
                columns: new[] { "AppUserId", "DeviceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AppUserDevices_AppUserId_IsTrusted_TrustedUntil",
                table: "AppUserDevices",
                columns: new[] { "AppUserId", "IsTrusted", "TrustedUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserProfiles_AppUserId",
                table: "AppUserProfiles",
                column: "AppUserId",
                unique: true,
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserSessions_AppUserId_DeviceFingerprint_IsActive",
                table: "AppUserSessions",
                columns: new[] { "AppUserId", "DeviceFingerprint", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserSessions_AppUserId_IsActive_ExpiresAt",
                table: "AppUserSessions",
                columns: new[] { "AppUserId", "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserSessions_IsActive_IsRevoked_ExpiresAt",
                table: "AppUserSessions",
                columns: new[] { "IsActive", "IsRevoked", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AppUserTotpSecrets_AppUserId_IsActive_ExpiresAt",
                table: "AppUserTotpSecrets",
                columns: new[] { "AppUserId", "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Fido2Credentials_CredentialId",
                table: "Fido2Credentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fido2Credentials_TenantId",
                table: "Fido2Credentials",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Fido2Credentials_UserId",
                table: "Fido2Credentials",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_MenuIcons_Key",
                table: "MenuIcons",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_ParentId",
                table: "MenuItems",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Placement_Parent_Department_Title",
                table: "MenuItems",
                columns: new[] { "Placement", "ParentId", "Title" });

            migrationBuilder.CreateIndex(
                name: "UX_MenuItems_Key",
                table: "MenuItems",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuPlacements_Key",
                table: "MenuPlacements",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuRoutes_Key",
                table: "MenuRoutes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_MenuRoutes_Url",
                table: "MenuRoutes",
                column: "Url",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyChallenges_ExpiresAtUtc",
                table: "PasskeyChallenges",
                column: "ExpiresAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PasskeyChallenges_TenantId_Key",
                table: "PasskeyChallenges",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PermissionActions_Key",
                table: "PermissionActions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_PermissionContexts_Key",
                table: "PermissionContexts",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionResources_Key",
                table: "PermissionResources",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "UX_PermissionResources_Context_Key",
                table: "PermissionResources",
                columns: new[] { "ContextKey", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Permissions_Key",
                table: "Permissions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_AppUserId_ExpiresAt_RevokedAt",
                table: "RefreshTokens",
                columns: new[] { "AppUserId", "ExpiresAt", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenFamily_AppUserId",
                table: "RefreshTokens",
                columns: new[] { "TokenFamily", "AppUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleClaims_RoleId",
                table: "RoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_IsDeleted",
                table: "Roles",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TempTotpSecrets_UserId_IsDeleted_ExpiresAt",
                table: "TempTotpSecrets",
                columns: new[] { "UserId", "IsDeleted", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserClaims_UserId",
                table: "UserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserLogins_UserId",
                table: "UserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "Users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_TenantId",
                table: "Users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AppUsers_TenantId_IsActive_IsDeleted",
                table: "Users",
                columns: new[] { "TenantId", "IsActive", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "Users",
                column: "NormalizedUserName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserDevices");

            migrationBuilder.DropTable(
                name: "AppUserProfiles");

            migrationBuilder.DropTable(
                name: "AppUserSessions");

            migrationBuilder.DropTable(
                name: "AppUserTotpSecrets");

            migrationBuilder.DropTable(
                name: "Fido2Credentials");

            migrationBuilder.DropTable(
                name: "MenuIcons");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "MenuPlacements");

            migrationBuilder.DropTable(
                name: "MenuRoutes");

            migrationBuilder.DropTable(
                name: "PasskeyChallenges");

            migrationBuilder.DropTable(
                name: "PermissionActions");

            migrationBuilder.DropTable(
                name: "PermissionContexts");

            migrationBuilder.DropTable(
                name: "PermissionResources");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RoleClaims");

            migrationBuilder.DropTable(
                name: "TempTotpSecrets");

            migrationBuilder.DropTable(
                name: "UserClaims");

            migrationBuilder.DropTable(
                name: "UserLogins");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "UserTokens");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
