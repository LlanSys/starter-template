# Configuration Code Conventions

This document defines how application configuration values should be consumed in code.

## Typed Mode And Provider Selection

Configuration values may remain strings in JSON, user-secrets, environment variables, and Key Vault because that is how .NET configuration works. Once those values enter code, parse mode/provider/transport values once into a typed enum and branch on the enum.

Do this:

```csharp
var mode = GetSomeMode(settings);

switch (mode)
{
    case SomeMode.Local:
        ConfigureLocal();
        break;

    case SomeMode.Azure:
        ConfigureAzure(settings);
        break;

    case SomeMode.Invalid:
    default:
        throw new InvalidOperationException(
            $"SomeSection:Mode '{settings.Mode}' is not supported. Supported values: Local, Azure.");
}
```

Avoid this:

```csharp
if (settings.Mode == "Azure" || settings.Mode == "Cloud")
{
    // ...
}
else
{
    // silently falls back to local
}
```

## Rules

- Each public settings POCO must live in its own file named after the type. Do not bundle nested settings classes in the parent settings file.
- Provider integrations must keep provider-specific request and response DTOs beside the provider adapter. Shared application contracts should expose canonical business inputs/results only, not Stripe, M-Pesa, Azure, or other provider wire payloads.
- Per-operation provider selection must go through a router or factory that delegates to isolated provider adapters. The configured provider is the default fallback, not a reason to hard-wire a single implementation at DI startup.
- Parse string configuration modes in one helper such as `GetKeyEncryptionMode` or `GetMessagingTransport`.
- Use a small enum for each bounded configuration choice.
- Keep each configuration branch in a one-purpose method.
- Fail fast on unsupported values with a message that names the section key and supported values.
- Do not silently fall back to a default when a non-empty value is invalid.
- Defaults are allowed only when the value is missing or blank and the default is documented.
- Keep auto-resolution explicit. For example, `Auto` can prefer Key Vault, then certificate, then no extra encryption, but this priority must live in a named method.

## Where Enums Belong

Not every enum belongs in the Domain layer.

Put enums in Domain only when they describe business language or invariants that belong to the model, such as customer status, gender, account state, role scope, approval state, or loan status.

Keep enums in the owning technical layer when they describe infrastructure, hosting, storage, transport, framework, or configuration choices. These are implementation details, even when the possible values are stable. Examples include Data Protection encryption mode, profile-image storage provider, auth provider, cache provider, and messaging transport.

Use this rule of thumb:

- If a business expert would use the term in a process discussion, it is probably Domain or SharedKernel.
- If only developers, DevOps, Azure, framework setup, or hosting configuration care about it, keep it in the owning layer.
- If the enum is only used to parse one settings POCO, keep it private near the parser.
- If the enum must cross API/UI boundaries, expose a DTO-friendly value and use the established enum conversion helpers at the boundary.

## Current Examples

- `DataProtection:KeyEncryptionMode` is parsed into `KeyEncryptionMode`.
- `Messaging:Transport` is parsed into `MessagingTransport`.
- `ProfileImageStorage:Provider` is parsed into `ProfileImageStorageProvider`.
- `AuthProvider:Provider` is parsed into `AuthProvider`.
- `EmailSettings:Provider` is parsed into `EmailProvider`.
- `Payments:Provider` is parsed into `PaymentProviderKind`, while `PaymentInitiationRequest.Provider` can override the default per payment.

When adding a new configurable provider, transport, mode, or strategy, follow the same pattern and update this document if the convention changes.
