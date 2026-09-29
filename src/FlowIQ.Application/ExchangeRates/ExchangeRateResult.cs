namespace FlowIQ.Application.ExchangeRates;

/// <param name="Source">Where the rate came from; some sources require attribution wherever the rate is shown.</param>
public record ExchangeRateResult(decimal? Rate, bool IsLive, string? Source);
