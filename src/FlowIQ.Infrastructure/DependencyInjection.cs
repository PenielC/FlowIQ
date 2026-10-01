using FlowIQ.Infrastructure.Analytics;
using FlowIQ.Application.Analytics;
using System.Net.Http.Headers;
using FlowIQ.Application.Authentication;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.CashFlowForecasting;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.CompaniesAndTeams;
using FlowIQ.Application.Customers;
using FlowIQ.Application.Invoicing;
using FlowIQ.Application.Invoicing.Emails;
using FlowIQ.Application.ProductUpdates;
using FlowIQ.Application.Blog;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Infrastructure.Authentication;
using FlowIQ.Infrastructure.Common;
using FlowIQ.Infrastructure.Email;
using FlowIQ.Infrastructure.ExchangeRates;
using FlowIQ.Infrastructure.Persistence;
using FlowIQ.Infrastructure.Repositories;
using FlowIQ.Infrastructure.StripeSubscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowIQ.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPlannedOwnerDrawRepository, PlannedOwnerDrawRepository>();
        services.AddScoped<IInvoiceEmailRepository, InvoiceEmailRepository>();
        services.AddScoped<IInvoiceReminderStore, InvoiceReminderStore>();
        services.AddScoped<IProductUpdateRepository, ProductUpdateRepository>();
        services.AddScoped<IUsageStore, UsageStore>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IAppUrlProvider, AppUrlProvider>();
        services.AddSingleton<IPlatformAdminChecker, PlatformAdminChecker>();
        services.AddSingleton<IFeedbackSettings, FeedbackSettings>();
        services.AddScoped<IStripeGateway, StripeGateway>();

        // Frankfurter first (with history), then ExchangeRate-API for the currencies Frankfurter lacks.
        services.AddHttpClient<FrankfurterExchangeRateProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.frankfurter.dev/v1/");
            client.Timeout = TimeSpan.FromSeconds(8);
        });
        services.AddHttpClient<OpenExchangeRateApiProvider>(client =>
        {
            client.BaseAddress = new Uri("https://open.er-api.com/v6/");
            client.Timeout = TimeSpan.FromSeconds(8);
        });
        services.AddScoped<IExchangeRateProvider, CompositeExchangeRateProvider>();

        if (!string.IsNullOrWhiteSpace(configuration["Email:SmtpHost"]))
        {
            // Local testing against a catch-all inbox (Mailpit); production leaves this unset and uses Resend.
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, client) =>
            {
                client.BaseAddress = new Uri("https://api.resend.com/");
                var apiKey = sp.GetRequiredService<IConfiguration>()["Email:ResendApiKey"];
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            });
        }

        return services;
    }
}
