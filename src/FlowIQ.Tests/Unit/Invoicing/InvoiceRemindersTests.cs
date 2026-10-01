using AwesomeAssertions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.Customers;
using FlowIQ.Application.Invoicing;
using FlowIQ.Application.Invoicing.Commands.CreateInvoice;
using FlowIQ.Application.Invoicing.Emails;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Customers;
using FlowIQ.Domain.Exceptions;
using FlowIQ.Domain.Invoicing;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FlowIQ.Tests.Unit.Invoicing;

public class InvoiceRemindersTests
{
    private static readonly DateTime Today = new(2026, 10, 20, 9, 0, 0, DateTimeKind.Utc);

    private static Invoice MakeInvoice(Guid companyId, DateTime due, string? email = "pay@client.example", decimal amount = 500m)
    {
        var invoice = new Invoice(companyId, "Sunrise Lodge", [("Conference lunch", amount)], due.AddDays(-14), due, InvoiceStatus.Sent, "USD", 1m, null);
        invoice.SetCustomerEmail(email);
        return invoice;
    }

    // ---------------------------------------------------------------- domain

    [Fact]
    public void Invoice_PastDue_BecomesOverdue_ButNotBeforeOrWhenPaid()
    {
        var company = Guid.NewGuid();
        var late = MakeInvoice(company, Today.AddDays(-1));
        var dueToday = MakeInvoice(company, Today.Date);
        var paid = MakeInvoice(company, Today.AddDays(-5));
        paid.MarkAsPaid();

        late.MarkOverdueIfPastDue(Today).Should().BeTrue();
        late.Status.Should().Be(InvoiceStatus.Overdue);
        dueToday.MarkOverdueIfPastDue(Today).Should().BeFalse();
        paid.MarkOverdueIfPastDue(Today).Should().BeFalse();
        paid.Status.Should().Be(InvoiceStatus.Paid);
    }

    [Fact]
    public void Invoice_PublicToken_IsCreatedOnce_AndIsUrlSafe()
    {
        var invoice = MakeInvoice(Guid.NewGuid(), Today);
        invoice.PublicToken.Should().BeNull();

        var token = invoice.EnsurePublicToken();

        token.Should().MatchRegex("^[A-Za-z0-9_-]{32}$");
        invoice.EnsurePublicToken().Should().Be(token);
    }

    [Fact]
    public void Invoice_CustomerEmail_IsTrimmedLowercasedOrCleared()
    {
        var invoice = MakeInvoice(Guid.NewGuid(), Today, "  Accounts@Client.Example ");
        invoice.CustomerEmail.Should().Be("accounts@client.example");
        invoice.SetCustomerEmail("   ");
        invoice.CustomerEmail.Should().BeNull();
    }

    [Theory]
    [InlineData(new int[0])]
    [InlineData(new[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(new[] { 0 })]
    [InlineData(new[] { 400 })]
    public void Company_RejectsBadReminderDays(int[] days)
    {
        var company = new Company("Acme");

        var act = () => company.SetReminderSettings(true, days);

        act.Should().Throw<DomainException>();
        company.ReminderEnabled.Should().BeFalse();
    }

    [Fact]
    public void Company_StoresReminderDaysSortedWithoutDuplicates()
    {
        var company = new Company("Acme");

        company.SetReminderSettings(true, [14, 1, 7, 7]);

        company.ReminderEnabled.Should().BeTrue();
        company.ReminderDays.Should().Equal(1, 7, 14);
    }

    // ---------------------------------------------------------------- the reminder run

    private sealed class Harness
    {
        public readonly Mock<IInvoiceReminderStore> Store = new();
        public readonly Mock<IInvoiceEmailRepository> Emails = new();
        public readonly Mock<IEmailSender> Sender = new();
        public readonly Mock<IUnitOfWork> UnitOfWork = new();
        public readonly List<InvoiceEmail> Recorded = [];
        public readonly List<OutgoingEmail> Sent = [];
        public readonly Company Company = new("Tatenda Foods");

        public Harness(HashSet<(Guid, int)>? handled = null)
        {
            Company.SetReminderSettings(true, [1, 7, 14]);
            Store.Setup(s => s.CompaniesWithRemindersOnAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync([Company]);
            Emails.Setup(e => e.GetHandledReminderDaysAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(handled ?? []);
            Emails.Setup(e => e.AddAsync(It.IsAny<InvoiceEmail>(), It.IsAny<CancellationToken>()))
                .Callback<InvoiceEmail, CancellationToken>((e, _) => Recorded.Add(e)).Returns(Task.CompletedTask);
            Sender.Setup(s => s.SendAsync(It.IsAny<OutgoingEmail>(), It.IsAny<CancellationToken>()))
                .Callback<OutgoingEmail, CancellationToken>((m, _) => Sent.Add(m)).Returns(Task.CompletedTask);
        }

        public void Invoices(params Invoice[] invoices) =>
            Store.Setup(s => s.RemindableInvoicesAsync(Company.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(invoices.ToList());

        public InvoiceReminderRunner Runner()
        {
            var clock = new Mock<IDateTimeProvider>();
            clock.Setup(c => c.UtcNow).Returns(Today);
            var urls = new Mock<IAppUrlProvider>();
            urls.Setup(u => u.WebBaseUrl).Returns("https://app.finflow.test/");
            var mailer = new InvoiceMailer(Sender.Object, Emails.Object, urls.Object, clock.Object, NullLogger<InvoiceMailer>.Instance);
            return new InvoiceReminderRunner(Store.Object, Emails.Object, mailer, clock.Object, UnitOfWork.Object);
        }
    }

    [Fact]
    public async Task Run_SendsOnlyTheLatestDueReminder_AndSkipsEarlierOnes()
    {
        var h = new Harness();
        var invoice = MakeInvoice(h.Company.Id, Today.AddDays(-10)); // past days 1 and 7, not 14
        h.Invoices(invoice);

        var summary = await h.Runner().RunAsync(null, CancellationToken.None);

        summary.Should().Be(new ReminderRunSummary(0, 1, 0, 1));
        h.Sent.Should().ContainSingle();
        h.Sent[0].ToEmail.Should().Be("pay@client.example");
        h.Sent[0].Subject.Should().StartWith("Reminder: invoice INV-");
        h.Sent[0].FromName.Should().Be("Tatenda Foods via FinFlow");
        h.Sent[0].HtmlBody.Should().Contain("https://app.finflow.test/i/" + invoice.PublicToken).And.Contain("10 days overdue");
        h.Recorded.Should().Contain(e => e.ReminderDay == 7 && e.Status == InvoiceEmailStatus.Sent)
            .And.Contain(e => e.ReminderDay == 1 && e.Status == InvoiceEmailStatus.Skipped);
        h.UnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_DoesNotRepeatAReminderDayAlreadyHandled()
    {
        var invoice = MakeInvoice(Guid.Empty, Today.AddDays(-3));
        var h = new Harness([(invoice.Id, 1)]);
        h.Invoices(invoice);

        var summary = await h.Runner().RunAsync(null, CancellationToken.None);

        summary.Sent.Should().Be(0);
        h.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Run_RecordsAFailedSend_SoItIsTriedAgainNextTime()
    {
        var h = new Harness();
        h.Sender.Setup(s => s.SendAsync(It.IsAny<OutgoingEmail>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Resend down"));
        h.Invoices(MakeInvoice(h.Company.Id, Today.AddDays(-1)));

        var summary = await h.Runner().RunAsync(null, CancellationToken.None);

        summary.Failed.Should().Be(1);
        h.Recorded.Should().ContainSingle(e => e.Status == InvoiceEmailStatus.Failed && e.Error == "Resend down" && e.ReminderDay == 1);
    }

    [Fact]
    public async Task Run_MarksOverdueEvenWhenNoCompanyHasRemindersOn()
    {
        var h = new Harness();
        h.Store.Setup(s => s.MarkOverdueAsync(Today.Date, null, It.IsAny<CancellationToken>())).ReturnsAsync(4);
        h.Store.Setup(s => s.CompaniesWithRemindersOnAsync(It.IsAny<Guid?>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var summary = await h.Runner().RunAsync(null, CancellationToken.None);

        summary.MarkedOverdue.Should().Be(4);
        h.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Run_NotYetAtTheFirstReminderDay_SendsNothing()
    {
        var h = new Harness();
        h.Company.SetReminderSettings(true, [3]);
        h.Invoices(MakeInvoice(h.Company.Id, Today.AddDays(-2)));

        await h.Runner().RunAsync(null, CancellationToken.None);

        h.Sent.Should().BeEmpty();
        h.Recorded.Should().BeEmpty();
    }

    // ---------------------------------------------------------------- composer

    [Fact]
    public void Composer_EscapesWhatPeopleTyped()
    {
        var invoice = new Invoice(Guid.NewGuid(), "<script>x</script> Ltd", [("Fish & chips <b>", 12.5m)], Today, Today.AddDays(7), InvoiceStatus.Sent, "ZAR", 1m, null);

        var email = InvoiceEmailComposer.Invoice(invoice, "Bites & Co", "https://app.finflow.test/i/abc", "Thanks <3");

        email.Html.Should().NotContain("<script>").And.Contain("&lt;script&gt;").And.Contain("Fish &amp; chips &lt;b&gt;")
            .And.Contain("Thanks &lt;3").And.Contain("ZAR 12.50");
        email.Text.Should().Contain("https://app.finflow.test/i/abc");
        email.Subject.Should().Contain("Bites & Co").And.Contain("ZAR 12.50");
    }

    // ---------------------------------------------------------------- creating an invoice picks up the customer's email

    [Fact]
    public async Task CreateInvoice_UsesTheSavedCustomersEmail_WhenNoneIsGiven()
    {
        var company = new Company("Acme");
        var companies = new Mock<IRepository<Company>>();
        companies.Setup(r => r.GetByIdAsync(company.Id, It.IsAny<CancellationToken>())).ReturnsAsync(company);
        var customers = new Mock<ICustomerRepository>();
        customers.Setup(r => r.FindByNameAsync(company.Id, "Sunrise Lodge", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer(company.Id, "Sunrise Lodge", "Accounts@Sunrise.Example", null, null));
        var handler = new CreateInvoiceCommandHandler(new Mock<IInvoiceRepository>().Object, customers.Object, companies.Object,
            new Mock<IExchangeRateProvider>().Object, new Mock<IUnitOfWork>().Object, NullLogger<CreateInvoiceCommandHandler>.Instance);

        var result = await handler.Handle(
            new CreateInvoiceCommand(company.Id, "Sunrise Lodge", [new("Lunch", 100m)], Today, Today.AddDays(7), company.Currency, null, null),
            CancellationToken.None);

        result.CustomerEmail.Should().Be("accounts@sunrise.example");
    }
}
