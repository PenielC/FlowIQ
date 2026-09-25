using FlowIQ.Application.Authentication;
using FlowIQ.Application.BankTransactions;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Application.CompaniesAndTeams;
using FlowIQ.Application.Customers;
using FlowIQ.Application.Invoicing;
using FlowIQ.Application.StripeSubscriptions;
using FlowIQ.Infrastructure.Authentication;
using FlowIQ.Infrastructure.Common;
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
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IAppUrlProvider, AppUrlProvider>();
        services.AddScoped<IStripeGateway, StripeGateway>();

        services.AddHttpClient<IExchangeRateProvider, FrankfurterExchangeRateProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.frankfurter.app/");
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        return services;
    }
}
