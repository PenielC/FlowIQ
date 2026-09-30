using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CashFlowForecasting;

namespace FlowIQ.Application.CashFlowForecasting;

public interface IPlannedOwnerDrawRepository : IRepository<PlannedOwnerDraw>
{
    /// <summary>The company's planned draws, tracked, ordered by next date.</summary>
    Task<List<PlannedOwnerDraw>> ListByCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
}
