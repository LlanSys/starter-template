using ST.Domain.Features.HR.Departments.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.HR.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public class HrTenantIsolationTests_Postgres : HrTenantIsolationTests<PostgreSqlDbFixture>
{
    public HrTenantIsolationTests_Postgres(PostgreSqlDbFixture fixture) : base(fixture) { }
}
