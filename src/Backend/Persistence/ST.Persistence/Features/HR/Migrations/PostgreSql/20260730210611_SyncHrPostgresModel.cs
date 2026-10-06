using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ST.Persistence.Features.HR.Migrations.PostgreSql
{
    /// <inheritdoc />
    public partial class SyncHrPostgresModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IdNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false, defaultValue: "+254"),
                    PhoneNationalNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: ""),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ManagerId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeNumberSequences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeNumberSequences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeNumberSequences_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "Code", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Description", "IsActive", "IsDeleted", "Name", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("0194f800-0000-7000-8000-000000000100"), "HR", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "People operations, employee lifecycle, and culture.", true, false, "Human Resources", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f800-0000-7000-8000-000000000200"), "FIN", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Finance operations, reporting, controls, and treasury.", true, false, "Finance", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f800-0000-7000-8000-000000000300"), "IT", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Platforms, systems administration, security, and support.", true, false, "Information Technology", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f800-0000-7000-8000-000000000400"), "LEGAL", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Legal advisory, compliance, contracts, and governance.", true, false, "Legal", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f800-0000-7000-8000-000000000500"), "OPS", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, "Day-to-day operations, service delivery, and process excellence.", true, false, "Operations", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "CountryCode", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "DepartmentId", "Email", "FirstName", "IdNumber", "IsDeleted", "LastName", "ManagerId", "Number", "PhoneNationalNumber", "PhoneNumber", "TenantId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { new Guid("0194f800-0000-7000-8000-000000000001"), "+254", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, new Guid("0194f800-0000-7000-8000-000000000100"), "aamodhiambo@gmail.com", "Alex", "14042262", false, "Odhiambo", new Guid("00000000-0000-0000-0000-000000000000"), "EMP-001", "798980115", "+254798980115", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f800-0000-7000-8000-000000000002"), "+254", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, new Guid("0194f800-0000-7000-8000-000000000200"), "allan.alex0803@gmail.com", "Allan", "67532424", false, "Alex", new Guid("0194f800-0000-7000-8000-000000000001"), "EMP-002", "700057578", "+254700057578", new Guid("0194f700-0000-7000-8000-000000000001"), null, null },
                    { new Guid("0194f800-0000-7000-8000-000000000003"), "+254", new DateTimeOffset(new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "System", null, null, new Guid("0194f800-0000-7000-8000-000000000300"), "omitolaura469@gmail.com", "Laura", "76945774", false, "Omito", new Guid("0194f800-0000-7000-8000-000000000001"), "EMP-003", "719423686", "+254719423686", new Guid("0194f700-0000-7000-8000-000000000001"), null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Code",
                table: "Departments",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeNumberSequences_DepartmentId",
                table: "EmployeeNumberSequences",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeNumberSequences_TenantId_DepartmentId_Year",
                table: "EmployeeNumberSequences",
                columns: new[] { "TenantId", "DepartmentId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Number",
                table: "Employees",
                column: "Number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeNumberSequences");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Departments");
        }
    }
}
