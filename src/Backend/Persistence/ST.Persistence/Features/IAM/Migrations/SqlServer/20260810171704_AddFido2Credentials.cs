using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ST.Persistence.Features.IAM.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class AddFido2Credentials : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Fido2Credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PublicKey = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UserHandle = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    CredentialId = table.Column<byte[]>(type: "varbinary(900)", nullable: false),
                    SignatureCounter = table.Column<long>(type: "bigint", nullable: false),
                    CredType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AaGuid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Fido2Credentials");
        }
    }
}
