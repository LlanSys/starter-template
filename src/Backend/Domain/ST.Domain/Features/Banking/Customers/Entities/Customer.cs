using ST.Domain.Features.Banking.Contracts;
using ST.Domain.Features.HR.Contracts;
using ST.Domain.Features.IAM.Contracts;
using ST.Domain.Shared.Contracts;
using ST.Domain.Shared.Contracts.Common;
using ST.Domain.Features.Banking.Customers.Enums;
using ST.Domain.Features.HR.Employees.Entities;
using ST.Domain.Shared.Entities;
using ST.Domain.Features.Banking.Customers.Events;
using ST.Domain.Exceptions;
using ST.Domain.Features.Banking.Customers.ValueObjects;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.Entities;

/// <summary>
    /// Customer is the aggregate root for corporate customer onboarding.
/// All state changes go through this entity — nothing is set directly
/// on owned entities from outside the aggregate.
/// </summary>
public class Customer : BaseEntity, ISoftDeletable, IHasDomainEvents
{
    // Header Info
    public string Number { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public CustomerType Type { get; private set; }
    public SegmentType SegmentType { get; private set; }
    public SubSegmentType SubSegmentType { get; private set; }
    public CustomerStatus Status { get; private set; }

    // Management
    public DateTimeOffset OpenedOn { get; private set; }
    public Guid RelationshipManagerId { get; private set; }
    public Employee? RelationshipManager { get; private set; }

    // Navigation Properties for Tabs
    public CorporateDetail CorporateDetail { get; private set; } = null!;
    public Address Address { get; private set; } = null!;
    public CommunicationPreference CommunicationPreference { get; private set; } = null!;

    private readonly List<Director> _directors = [];
    public IReadOnlyCollection<Director> Directors => _directors.AsReadOnly();

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    private Customer() { }

    /// <summary>
    /// Factory method — the only way to create a valid Customer aggregate.
    /// Raises CustomerCreatedEvent on success.
    /// </summary>
    public static Customer Create(
        string number,
        string name,
        CustomerType type,
        SegmentType segmentType,
        SubSegmentType subSegmentType,
        Guid rmId,
        DateTimeOffset openedOn,
        CorporateDetail corporateDetail,
        Address address,
        CommunicationPreference communicationPreference,
        string createdBy
    )
    {
        var customer = new Customer
        {
            Id = Guid.CreateVersion7(), // The Id is created...
            Number = number,
            Name = name,
            Type = type,
            SegmentType = segmentType,
            SubSegmentType = subSegmentType,
            Status = CustomerStatus.Draft,
            RelationshipManagerId = rmId,
            OpenedOn = openedOn,
            CorporateDetail = corporateDetail,
            Address = address,
            CommunicationPreference = communicationPreference,
            CreatedBy = createdBy
        };

        var domainEvent = new CustomerCreatedEvent(
            customer.Id,
            customer.Number,
            customer.Name,
            customer.Address.Email ?? string.Empty,
            customer.Type
    );

        // Step 3: Raise the event on the newly created instance.
        customer.RaiseDomainEvent(domainEvent);

        // Step 4: Return the fully constructed customer with its pending domain event.
        return customer;
    }

    // -------------------------------------------------------------------------
    // Behaviour methods — all state mutations go through here
    // -------------------------------------------------------------------------

    public void SetCorporateDetails(CorporateDetail corporateDetails)
    {
        ArgumentNullException.ThrowIfNull(corporateDetails);
        CorporateDetail = corporateDetails;
    }

    public void SetAddress(Address address)
    {
        ArgumentNullException.ThrowIfNull(address);
        Address = address;
    }

    public void SetCommunicationPreferences(CommunicationPreference preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);
        CommunicationPreference = preferences;
    }

    public void AssignRelationshipManager(Guid relationshipManagerId)
    {
        if (relationshipManagerId == Guid.Empty)
            throw new DomainException("A Relationship Manager must be assigned.");

        RelationshipManagerId = relationshipManagerId;
    }

    public void SubmitForApproval()
    {
        if (Status != CustomerStatus.Draft)
            throw new DomainException("Only draft customers can be submitted for approval.");

        Status = CustomerStatus.PendingApproval;
    }

    public void Activate()
    {
        if (Status is not CustomerStatus.PendingApproval and not CustomerStatus.Suspended)
            throw new DomainException("Only pending or suspended customers can be activated.");

        Status = CustomerStatus.Active;
    }

    public void Suspend()
    {
        if (Status != CustomerStatus.Active)
            throw new DomainException("Only active customers can be suspended.");

        Status = CustomerStatus.Suspended;
    }

    public void Close()
    {
        if (Status == CustomerStatus.Closed)
            throw new DomainException("Customer is already closed.");

        Status = CustomerStatus.Closed;
    }

    public void AddDirector(Director director)
    {
        ArgumentNullException.ThrowIfNull(director);

        var totalShares = _directors.Sum(d => d.SharePercentage) + director.SharePercentage;
        if (totalShares > 100)
        {
            throw new DomainException(
                $"Total share percentage cannot exceed 100%. Current total would be {totalShares}%.");
        }

        _directors.Add(director);
    }

    public void RemoveDirector(Guid directorId)
    {
        var director = _directors.FirstOrDefault(d => d.Id == directorId)
            ?? throw new DomainException("Director not found.");

        _directors.Remove(director);
    }

    public void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    public void MarkAsDeleted(string deletedBy)
    {
        ArgumentNullException.ThrowIfNull(deletedBy);
        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        DeletedBy = deletedBy;

    }

}
