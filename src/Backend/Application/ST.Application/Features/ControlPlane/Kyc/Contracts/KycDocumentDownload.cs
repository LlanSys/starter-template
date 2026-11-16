namespace ST.Application.Features.ControlPlane.Kyc.Contracts;

public sealed record KycDocumentDownload(Stream Content, string ContentType, string FileName);
