using System.Net;
using IPNetwork = System.Net.IPNetwork;
using Microsoft.AspNetCore.HttpOverrides;

namespace ST.Api.Configuration;

internal sealed class TrustedProxySettings
{
    public const string SectionName = "TrustedForwardedHeaders";

    public string[] Proxies { get; init; } = [];
    public string[] Networks { get; init; } = [];

    public void Apply(ForwardedHeadersOptions options)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.ForwardLimit = 1;
        options.KnownProxies.Clear();
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Add(IPAddress.Loopback);
        options.KnownProxies.Add(IPAddress.IPv6Loopback);

        foreach (var value in Proxies)
        {
            if (!IPAddress.TryParse(value, out var address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any))
            {
                throw new InvalidOperationException("TrustedForwardedHeaders contains an invalid proxy address.");
            }

            options.KnownProxies.Add(address);
        }

        foreach (var value in Networks)
        {
            if (!IPNetwork.TryParse(value, out var network) || network.PrefixLength == 0)
            {
                throw new InvalidOperationException("TrustedForwardedHeaders contains an invalid or unrestricted proxy network.");
            }

            options.KnownIPNetworks.Add(network);
        }
    }
}
