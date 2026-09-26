namespace FlowIQ.Api.Common;

public static class ClientPlatform
{
    public static string? Read(HttpRequest request) => request.Headers["X-Client-Platform"].FirstOrDefault();
}
