using Mediator;

namespace FlowIQ.Application.AiInsights.Queries.GetAiInsights;

public record GetAiInsightsQuery(Guid CompanyId) : IQuery<AiInsightsResult>;

public enum InsightTone
{
    Positive = 0,
    Warning = 1,
    Caution = 2,
}

public record Insight(string Title, string Description, InsightTone Tone);

public record AiInsightsResult(IReadOnlyCollection<Insight> Insights);
