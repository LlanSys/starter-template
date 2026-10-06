using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.SharedKernel.Dtos.Common;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Persistence.Features.HR.DataContext;

namespace ST.Tests.Integration.ApiTests;

public class TenantIsolationApiTests : IClassFixture<TestFixtures.StarterTemplateWebApplicationFactory>
{
    private readonly TestFixtures.StarterTemplateWebApplicationFactory _factory;

    public TenantIsolationApiTests(TestFixtures.StarterTemplateWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CrossTenantDataAccess_ShouldReturnNotFound()
    {
        // Arrange
        var tenantA_Id = Guid.NewGuid();
        var tenantB_Id = Guid.NewGuid();
        var rmId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            // Employee belongs to HrDBContext — not BankingDBContext
            var hrContext = scope.ServiceProvider.GetRequiredService<HrDBContext>();
            var employee = Employee.Create($"RM-{Guid.NewGuid().ToString()[..5]}", "rm@example.com", "RM", "User", "ID-123", "KE", "700000000", "+254700000000", Guid.NewGuid(), null, "Test");
            employee.GetType().GetProperty("Id")?.SetValue(employee, rmId);
            employee.GetType().GetProperty("TenantId")?.SetValue(employee, tenantA_Id);
            hrContext.Set<Employee>().Add(employee);
            await hrContext.SaveChangesAsync();
        }

        var clientA = _factory.CreateClient();
        clientA.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(TestFixtures.TestAuthHandler.AuthenticationScheme);
        clientA.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        clientA.DefaultRequestHeaders.Add("X-Test-TenantId", tenantA_Id.ToString());
        clientA.DefaultRequestHeaders.Add("X-Test-Permissions", "customers.create,customers.view");

        var clientB = _factory.CreateClient();
        clientB.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(TestFixtures.TestAuthHandler.AuthenticationScheme);
        clientB.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        clientB.DefaultRequestHeaders.Add("X-Test-TenantId", tenantB_Id.ToString());
        clientB.DefaultRequestHeaders.Add("X-Test-Permissions", "customers.view");

        // Act 1: Tenant A creates a customer
        var createRequest = new CreateCustomerRequest(
            Type: ST.Domain.Features.Banking.Customers.Enums.CustomerType.Individual.ToString(),
            SegmentType: ST.Domain.Features.Banking.Customers.Enums.SegmentType.Retail.ToString(),
            SubSegmentType: ST.Domain.Features.Banking.Customers.Enums.SubSegmentType.Local.ToString(),
            Classification: "Gold",
            CompanyName: "Tenant A Company",
            LineOfBusiness: ST.Domain.Features.Banking.Customers.Enums.LineOfBusiness.Agriculture.ToString(),
            LineOfBusinessMoreInfo: "Crop Farming",
            NatureOfBusiness: "Farming",
            IdentificationType: ST.Domain.Features.Banking.Customers.Enums.IdentificationType.CertificateOfIncorporation.ToString(),
            RegistrationNumber: "REG-123",
            DateOfRegistration: DateTimeOffset.UtcNow.AddYears(-5),
            RegisteredAt: "City",
            RegisteredOffice: "Office A",
            BusinessStartedYear: 2020,
            NumberOfEmployees: 10,
            Comments: null,
            Website: null,
            TINNumber: null,
            RelationshipManagerId: rmId, // Will set to existing employee ID
            OpenedOn: DateTimeOffset.UtcNow,
            ResidentialAddress: "123 Main St",
            Country: "US",
            Region: "NY",
            Ward: "Ward1",
            District: "D1",
            BusinessAddress: null,
            OfficeAddress: null,
            MailingAddress: null,
            Street: null,
            ZipCode: null,
            PhoneHome: null,
            PhoneWork: null,
            Mobile: "555-0100",
            FaxNo: null,
            LandMark: null,
            Email: "tenantA.customer@example.com",
            CanSendGreetings: true,
            CanSendAssociateSpecialOffer: false,
            CanSendOurSpecialOffers: false,
            StatementOnline: true,
            MobileAlert: true
        );

        var response = await clientA.PostAsJsonAsync("/api/v1/banking/customers", createRequest);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Creation failed with {response.StatusCode}: {content}");
        }

        // Verify the response
        response.EnsureSuccessStatusCode();
        var createdCustomer = await response.Content.ReadFromJsonAsync<AppResponse<CustomerResponse>>();

        Assert.NotNull(createdCustomer);
        Assert.True(createdCustomer.IsSuccess);
        var customerId = createdCustomer.Data!.Id;

        // Act 2: Tenant A gets the customer
        var getResponseA = await clientA.GetAsync($"/api/v1/banking/customers/{customerId}");
        getResponseA.EnsureSuccessStatusCode();

        // Act 3: Tenant B tries to get Tenant A's customer
        var getResponseB = await clientB.GetAsync($"/api/v1/banking/customers/{customerId}");

        // Assert: Tenant B should get a 404 because of global query filters
        Assert.Equal(HttpStatusCode.NotFound, getResponseB.StatusCode);
    }
}
