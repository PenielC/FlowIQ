namespace FlowIQ.Application.Common.Interfaces;

/// <summary>The web app's base URL, for building redirect links from backend handlers (e.g. Stripe Checkout).</summary>
public interface IAppUrlProvider
{
    string WebBaseUrl { get; }
}
