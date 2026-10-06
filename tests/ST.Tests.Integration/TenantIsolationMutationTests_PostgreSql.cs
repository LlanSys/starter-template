using ST.Domain.Features.IAM.Menus.Entities;
using ST.Domain.Shared.Contracts.Common;
using ST.Persistence.Features.IAM.DataContext;
using ST.Tests.Integration.TestFixtures;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ST.Tests.Integration;

public class TenantIsolationMutationTests_PostgreSql : TenantIsolationMutationTests<PostgreSqlDbFixture>
{
    public TenantIsolationMutationTests_PostgreSql(PostgreSqlDbFixture fixture) : base(fixture) { }
}
