using ST.Domain.Features.Banking.Customers.Contracts.Specifications;
using ST.Domain.Shared.Contracts.Specifications;
using ST.Domain.Features.Banking.Customers.Entities;
using ST.Domain.Features.Banking.Customers.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Contracts.Specifications;

public class CustomerSearchSpec : Specification<Customer, Guid>, ICustomerSearchSpec
{
    public CustomerSearchSpec(
        string? globalSearch,
        CustomerType? type,
        SegmentType? segmentType,
        SubSegmentType? subSegmentType,
        IdentificationType? identificationType,
        LineOfBusiness? lineOfBusiness,
        CustomerStatus? status,
        Guid? relationshipManagerId,
        Guid? cursor,
        int pageSize

    )
    {
        AddCriteria(customer =>
            (string.IsNullOrWhiteSpace(globalSearch) ||
                customer.Number.Contains(globalSearch) ||
                customer.CorporateDetail.CompanyName.Contains(globalSearch) ||
                customer.CorporateDetail.RegistrationNumber.Contains(globalSearch) ||
                (customer.CorporateDetail.TINNumber != null && customer.CorporateDetail.TINNumber.Contains(globalSearch)) ||
                (customer.Address.Mobile != null && customer.Address.Mobile.Contains(globalSearch)) ||
                (customer.Address.Email != null && customer.Address.Email.Contains(globalSearch)))

            && (!type.HasValue || customer.Type == type.Value)
            && (!segmentType.HasValue || customer.SegmentType == segmentType.Value)
            && (!subSegmentType.HasValue || customer.SubSegmentType == subSegmentType.Value)
            && (!identificationType.HasValue || customer.CorporateDetail.IdentificationType == identificationType.Value)
            && (!lineOfBusiness.HasValue || customer.CorporateDetail.LineOfBusiness == lineOfBusiness.Value)
            && (!status.HasValue || customer.Status == status.Value)
            && (!relationshipManagerId.HasValue || customer.RelationshipManagerId == relationshipManagerId.Value)
        );

        AddInclude(c => c.RelationshipManager!);
        AddOrderBy(c => c.Id);

        // Only set the cursor if it actually exists. If null, the Evaluator knows to start from the beginning.
        if (cursor.HasValue && cursor.Value != Guid.Empty)
        {
            SetCursor(cursor.Value, c => c.Id > cursor.Value);
            //SetCursor(cursor.Value, "Id");
        }

        SetTake(Math.Clamp(pageSize, 1, 51)); // permits pageSize + 1 cursor detection.
        EnableSplitQuery();
    }

}
