using ST.Domain.Features.HR.Employees.Entities;
using ST.Persistence.Features.HR.Employees.Seeds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Persistence.Features.HR.Employees.EntityConfigurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Number)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(x => x.Number)
            .IsUnique()
            .HasDatabaseName("IX_Employees_Number");

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IdNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.CountryCode)
            .IsRequired()
            .HasMaxLength(8)
            .HasDefaultValue("+254");

        builder.Property(x => x.PhoneNationalNumber)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(string.Empty);

        builder.Property(x => x.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        // Audit columns
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        // Optimistic concurrency
        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsRequired();

        // Seed — wired here so the migration knows where the data belongs.
        builder.HasData(EmployeeSeed.GetSeedData());
    }
}
