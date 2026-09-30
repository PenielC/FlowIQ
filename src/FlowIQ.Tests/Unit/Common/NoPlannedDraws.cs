using FlowIQ.Application.CashFlowForecasting;
using Moq;

namespace FlowIQ.Tests.Unit.Common;

/// <summary>A planned-draw repository for a company that has none.</summary>
public static class NoPlannedDraws
{
    public static IPlannedOwnerDrawRepository Repository()
    {
        var mock = new Mock<IPlannedOwnerDrawRepository>();
        mock.Setup(r => r.ListByCompanyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        return mock.Object;
    }
}
