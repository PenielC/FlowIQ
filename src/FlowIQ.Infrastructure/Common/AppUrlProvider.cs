using FlowIQ.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FlowIQ.Infrastructure.Common;

public class AppUrlProvider(IConfiguration configuration) : IAppUrlProvider
{
    public string WebBaseUrl => configuration["App:WebBaseUrl"] ?? "http://localhost:5173";
}
