using ST.Application.Contracts.Interfaces.Common;
using ST.SharedKernel.Extensions;
using ST.Application.Features.Banking.Customers.Mappings;
using ST.Application.Features.HR.Employees.Mappings;
using ST.Application.Features.IAM.Users.Mappings;
using ST.Application.Features.Shared.EmailTemplates.Mappings;
using ST.Application.Utilities;
using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Entities;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.Domain.Features.Banking.Customers.ValueObjects;
using ST.SharedKernel.Features.Banking.Customers.Dtos;
using ST.SharedKernel.Dtos.Common;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Application.Features.Banking.Customers.CommandHandlers;


/// <summary>
/// Invalidation:
///   - Direct:  delete the entity entry so the next GetById call fetches fresh data.
///   - Version: bump the global "customers" and "dashboard" versions to orphan list entries.
///
/// Both are necessary: without the direct deletion the entity detail page would
/// still show stale data even after the list refreshes.
/// </summary>


internal sealed class UpdateCustomerCommandHandler(
    IBankingUnitOfWork _bankingUnitOfWork,
    IHrUnitOfWork _hrUnitOfWork,
    ILogger<UpdateCustomerCommandHandler> _logger)
    : IRequestHandler<UpdateCustomerCommand, AppResponse<CustomerResponse>>
{

    public async Task<AppResponse<CustomerResponse>> Handle(UpdateCustomerCommand command, CancellationToken ct)
    {
        try
        {
            var req = command.UpdateCustomerRequest;
            var customer = await _bankingUnitOfWork.CustomerRepository.FindByIdAsync(req.Id, ct).ConfigureAwait(false);
            if (customer is null)
                return AppResponses.Failure<CustomerResponse>($"Customer {req.Id} not found.");

            // Verify new RM if it changed
            var rm = await _hrUnitOfWork.EmployeeRepository
                .FindByIdAsync(req.RelationshipManagerId, ct)
                .ConfigureAwait(false);

            if (rm is null)
                return AppResponses.Failure<CustomerResponse>("Selected Relationship Manager does not exist or is inactive.");

            // Update via aggregate behaviours — never set properties directly
            customer.SetCorporateDetails(CorporateDetail.Create(
                req.CompanyName,
                req.LineOfBusiness.ToEnum<LineOfBusiness>(),
                req.NatureOfBusiness,
                req.IdentificationType.ToEnum<IdentificationType>(),
                req.RegistrationNumber,
                req.DateOfRegistration,
                req.LineOfBusinessMoreInfo,
                req.RegisteredAt,
                req.RegisteredOffice,
                req.BusinessStartedYear,
                req.NumberOfEmployees,
                req.Website,
                req.TINNumber,
                req.Classification,
                req.Comments

            ));

            customer.SetAddress(Address.Create(
                residentialAddress: req.ResidentialAddress,
                country: req.Country,
                region: req.Region,
                ward: req.Ward,
                district: req.District,
                mobile: req.Mobile,
                emailId: req.EmailId,
                businessAddress: req.BusinessAddress,
                officeAddress: req.OfficeAddress,
                mailingAddress: req.MailingAddress,
                street: req.Street,
                zipCode: req.ZipCode,
                phoneHome: req.PhoneHome,
                phoneWork: req.PhoneWork,
                faxNo: req.FaxNo,
                landMark: req.LandMark
            ));

            customer.SetCommunicationPreferences(CommunicationPreference.Create(
                canSendGreetings: req.CanSendGreetings,
                canSendAssociateSpecialOffer: req.CanSendAssociateSpecialOffer,
                canSendOurSpecialOffers: req.CanSendOurSpecialOffers,
                statementOnline: req.StatementOnline,
                mobileAlert: req.MobileAlert
            ));

            customer.AssignRelationshipManager(req.RelationshipManagerId);

            await _bankingUnitOfWork.CustomerRepository.UpdateAsync(customer, ct).ConfigureAwait(false);
            await _bankingUnitOfWork.CompleteAsync(ct).ConfigureAwait(false);

            LogDefinitions.LogCustomerUpdated(_logger, customer.Number);

            return AppResponses.Success("Customer updated successfully.", customer.ToCustomerResponse());
        }
        catch (Exception ex)
        {
            LogDefinitions.LogCustomerUpdateFailed(_logger, command.UpdateCustomerRequest.Id, ex);
            throw;
        }
    }
}
