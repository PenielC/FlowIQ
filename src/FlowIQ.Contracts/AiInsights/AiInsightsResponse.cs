namespace FlowIQ.Contracts.AiInsights;

public record InsightResponse(string Title, string Description, string Tone);

public record AiInsightsResponse(IReadOnlyCollection<InsightResponse> Insights);
