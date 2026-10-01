using FlowIQ.Application.Authentication;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;
using FluentValidation;
using Mediator;

namespace FlowIQ.Application.Invoicing.Emails;

public class SendInvoiceEmailCommandHandler(
    IInvoiceRepository invoiceRepository,
    IRepository<Company> companyRepository,
    IUserRepository userRepository,
    InvoiceMailer mailer,
    IUnitOfWork unitOfWork) : ICommandHandler<SendInvoiceEmailCommand, InvoiceEmailResult>
{
    public async ValueTask<InvoiceEmailResult> Handle(SendInvoiceEmailCommand command, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null || invoice.CompanyId != command.CompanyId)
        {
            throw new DomainException("Invoice not found.");
        }

        if (!string.IsNullOrWhiteSpace(command.ToEmail)) invoice.SetCustomerEmail(command.ToEmail);
        if (invoice.CustomerEmail is null)
        {
            throw new DomainException("Add the customer's email address first.");
        }

        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");
        // Replies go to the person who sent it.
        var sender = await userRepository.GetByIdAsync(command.UserId, cancellationToken);

        var record = await mailer.SendAsync(invoice, company, InvoiceEmailKind.Invoice, null, command.Message, sender?.Email, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (record.Status == InvoiceEmailStatus.Failed)
        {
            throw new DomainException("The email couldn't be sent. Please try again in a moment.");
        }

        return InvoiceEmailResult.From(record);
    }
}

public class SendInvoiceEmailCommandValidator : AbstractValidator<SendInvoiceEmailCommand>
{
    public SendInvoiceEmailCommandValidator()
    {
        RuleFor(x => x.ToEmail).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.ToEmail));
        RuleFor(x => x.Message).MaximumLength(2000);
    }
}

public class UpdateInvoiceDeliveryCommandHandler(IInvoiceRepository invoiceRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateInvoiceDeliveryCommand, InvoiceResult>
{
    public async ValueTask<InvoiceResult> Handle(UpdateInvoiceDeliveryCommand command, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (invoice is null || invoice.CompanyId != command.CompanyId)
        {
            throw new DomainException("Invoice not found.");
        }

        invoice.SetCustomerEmail(command.CustomerEmail);
        invoice.SetRemindersPaused(command.RemindersPaused);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return InvoiceResult.From(invoice);
    }
}

public class UpdateInvoiceDeliveryCommandValidator : AbstractValidator<UpdateInvoiceDeliveryCommand>
{
    public UpdateInvoiceDeliveryCommandValidator()
    {
        RuleFor(x => x.CustomerEmail).EmailAddress().MaximumLength(256).When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));
    }
}

public class GetInvoiceEmailsQueryHandler(IInvoiceRepository invoiceRepository, IInvoiceEmailRepository invoiceEmails)
    : IQueryHandler<GetInvoiceEmailsQuery, IReadOnlyList<InvoiceEmailResult>>
{
    public async ValueTask<IReadOnlyList<InvoiceEmailResult>> Handle(GetInvoiceEmailsQuery query, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(query.InvoiceId, cancellationToken);
        if (invoice is null || invoice.CompanyId != query.CompanyId)
        {
            throw new DomainException("Invoice not found.");
        }

        var emails = await invoiceEmails.ListByInvoiceAsync(query.InvoiceId, cancellationToken);
        return emails.Select(InvoiceEmailResult.From).ToList();
    }
}

public class GetReminderSettingsQueryHandler(IRepository<Company> companyRepository)
    : IQueryHandler<GetReminderSettingsQuery, ReminderSettingsResult>
{
    public async ValueTask<ReminderSettingsResult> Handle(GetReminderSettingsQuery query, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(query.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");
        return new ReminderSettingsResult(company.ReminderEnabled, company.ReminderDays);
    }
}

public class UpdateReminderSettingsCommandHandler(IRepository<Company> companyRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<UpdateReminderSettingsCommand, ReminderSettingsResult>
{
    public async ValueTask<ReminderSettingsResult> Handle(UpdateReminderSettingsCommand command, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(command.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");
        company.SetReminderSettings(command.Enabled, command.Days);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReminderSettingsResult(company.ReminderEnabled, company.ReminderDays);
    }
}

public class UpdateReminderSettingsCommandValidator : AbstractValidator<UpdateReminderSettingsCommand>
{
    public UpdateReminderSettingsCommandValidator()
    {
        RuleFor(x => x.Days).NotEmpty().Must(d => d.Count <= 5).WithMessage("Choose between 1 and 5 reminder days.");
        RuleForEach(x => x.Days).InclusiveBetween(1, 365).WithMessage("Reminder days must be between 1 and 365 days after the due date.");
    }
}

public class RunInvoiceRemindersCommandHandler(InvoiceReminderRunner runner) : ICommandHandler<RunInvoiceRemindersCommand, ReminderRunSummary>
{
    public async ValueTask<ReminderRunSummary> Handle(RunInvoiceRemindersCommand command, CancellationToken cancellationToken) =>
        await runner.RunAsync(command.CompanyId, cancellationToken);
}

public class GetPublicInvoiceQueryHandler(IInvoiceRepository invoiceRepository, IRepository<Company> companyRepository)
    : IQueryHandler<GetPublicInvoiceQuery, PublicInvoiceResult>
{
    public async ValueTask<PublicInvoiceResult> Handle(GetPublicInvoiceQuery query, CancellationToken cancellationToken)
    {
        var invoice = string.IsNullOrWhiteSpace(query.Token) ? null : await invoiceRepository.GetByPublicTokenAsync(query.Token, cancellationToken);
        var company = invoice is null ? null : await companyRepository.GetByIdAsync(invoice.CompanyId, cancellationToken);
        if (invoice is null || company is null || !company.IsActive)
        {
            throw new KeyNotFoundException("This invoice link isn't valid. Please ask the business for a new one.");
        }

        var logo = company.LogoData is { Length: > 0 } && company.LogoContentType is not null
            ? $"data:{company.LogoContentType};base64,{Convert.ToBase64String(company.LogoData)}"
            : null;
        return new PublicInvoiceResult(
            company.Name, logo, InvoiceEmailComposer.InvoiceNumber(invoice.Id), invoice.CustomerName,
            invoice.IssueDateUtc, invoice.DueDateUtc, invoice.Status, invoice.Currency, invoice.Amount,
            invoice.LineItems.Select(li => new InvoiceLineItemResult(li.Id, li.Description, li.Amount)).ToList(),
            invoice.Notes);
    }
}
