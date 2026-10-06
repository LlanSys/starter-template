using System;
using System.Collections.Generic;
using System.Text;

namespace ST.Domain.Features.Banking.Customers.ValueObjects;

public class CommunicationPreference
{
    public bool CanSendGreetings { get; private set; }
    public bool CanSendAssociateSpecialOffer { get; private set; }
    public bool CanSendOurSpecialOffers { get; private set; }
    public bool StatementOnline { get; private set; }
    public bool MobileAlert { get; private set; }

    private CommunicationPreference() { }

    public static CommunicationPreference Create(
        bool canSendGreetings = false,
        bool canSendAssociateSpecialOffer = false,
        bool canSendOurSpecialOffers = false,
        bool statementOnline = false,
        bool mobileAlert = false)
    {
        return new CommunicationPreference
        {
            CanSendGreetings = canSendGreetings,
            CanSendAssociateSpecialOffer = canSendAssociateSpecialOffer,
            CanSendOurSpecialOffers = canSendOurSpecialOffers,
            StatementOnline = statementOnline,
            MobileAlert = mobileAlert
        };
    }

    internal void Update(
        bool canSendGreetings,
        bool canSendAssociateSpecialOffer,
        bool canSendOurSpecialOffers,
        bool statementOnline,
        bool mobileAlert)
    {
        CanSendGreetings = canSendGreetings;
        CanSendAssociateSpecialOffer = canSendAssociateSpecialOffer;
        CanSendOurSpecialOffers = canSendOurSpecialOffers;
        StatementOnline = statementOnline;
        MobileAlert = mobileAlert;
    }
}
