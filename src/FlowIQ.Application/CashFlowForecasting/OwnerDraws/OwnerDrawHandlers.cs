using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CashFlowForecasting;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Application.CashFlowForecasting.OwnerDraws;

public class GetOwnerDrawsQueryHandler(
    IPlannedOwnerDrawRepository drawRepository,
    IRepository<Company> companyRepository) : IQueryHandler<GetOwnerDrawsQuery, OwnerDrawsResult>
{
    public async ValueTask<OwnerDrawsResult> Handle(GetOwnerDrawsQuery query, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(query.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");
        var draws = await drawRepository.ListByCompanyAsync(query.CompanyId, cancellationToken);
        return new OwnerDrawsResult(company.ForecastSetupCompletedAtUtc is not null, draws.Select(OwnerDrawResult.From).ToList());
    }
}

public class CreateOwnerDrawCommandHandler(
    IPlannedOwnerDrawRepository drawRepository,
    IRepository<Company> companyRepository,
    IExchangeRateProvider exchangeRateProvider,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork,
    ILogger<CreateOwnerDrawCommandHandler> logger) : ICommandHandler<CreateOwnerDrawCommand, OwnerDrawResult>
{
    public async ValueTask<OwnerDrawResult> Handle(CreateOwnerDrawCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");
        var rate = await CurrencyConversion.ResolveRateAsync(
            exchangeRateProvider, logger, command.Currency, company.Currency, command.ExchangeRate, cancellationToken);

        var draw = new PlannedOwnerDraw(
            command.CompanyId, command.Name, command.Amount, command.Currency, Math.Round(command.Amount * rate, 2), rate,
            command.Frequency, command.NextDateUtc, command.Months);

        await drawRepository.AddAsync(draw, cancellationToken);
        // Adding a draw answers the setup question.
        company.CompleteForecastSetup(dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OwnerDrawResult.From(draw);
    }
}

public class UpdateOwnerDrawCommandHandler(
    IPlannedOwnerDrawRepository drawRepository,
    IRepository<Company> companyRepository,
    IExchangeRateProvider exchangeRateProvider,
    IUnitOfWork unitOfWork,
    ILogger<UpdateOwnerDrawCommandHandler> logger) : ICommandHandler<UpdateOwnerDrawCommand, OwnerDrawResult>
{
    public async ValueTask<OwnerDrawResult> Handle(UpdateOwnerDrawCommand command, CancellationToken cancellationToken)
    {
        var draw = await drawRepository.GetByIdAsync(command.DrawId, cancellationToken);
        if (draw is null || draw.CompanyId != command.CompanyId)
        {
            throw new DomainException("Planned withdrawal not found.");
        }

        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");
        var rate = await CurrencyConversion.ResolveRateAsync(
            exchangeRateProvider, logger, command.Currency, company.Currency, command.ExchangeRate, cancellationToken);

        draw.Update(command.Name, command.Amount, command.Currency, Math.Round(command.Amount * rate, 2), rate,
            command.Frequency, command.NextDateUtc, command.Months);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return OwnerDrawResult.From(draw);
    }
}

public class DeleteOwnerDrawCommandHandler(
    IPlannedOwnerDrawRepository drawRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteOwnerDrawCommand>
{
    public async ValueTask<Unit> Handle(DeleteOwnerDrawCommand command, CancellationToken cancellationToken)
    {
        var draw = await drawRepository.GetByIdAsync(command.DrawId, cancellationToken);
        if (draw is null || draw.CompanyId != command.CompanyId)
        {
            throw new DomainException("Planned withdrawal not found.");
        }

        drawRepository.Remove(draw);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

public class CompleteForecastSetupCommandHandler(
    IRepository<Company> companyRepository,
    IDateTimeProvider dateTimeProvider,
    IUnitOfWork unitOfWork) : ICommandHandler<CompleteForecastSetupCommand>
{
    public async ValueTask<Unit> Handle(CompleteForecastSetupCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");
        company.CompleteForecastSetup(dateTimeProvider.UtcNow);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
