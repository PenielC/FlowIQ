using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using FlowIQ.Application.AiCategorisation;
using FlowIQ.Application.Common;
using FlowIQ.Application.Common.Interfaces;
using FlowIQ.Domain.BankTransactions;
using FlowIQ.Domain.CompaniesAndTeams;
using FlowIQ.Domain.Exceptions;
using Mediator;
using Microsoft.Extensions.Logging;

namespace FlowIQ.Application.BankTransactions.TransactionImport.Queries.PreviewTransactionImport;

public class PreviewTransactionImportQueryHandler(
    ITransactionRepository transactionRepository,
    IRepository<Company> companyRepository,
    IExchangeRateProvider exchangeRateProvider,
    ILogger<PreviewTransactionImportQueryHandler> logger) : IQueryHandler<PreviewTransactionImportQuery, TransactionImportPreviewResult>
{
    private const int MaxRows = 1000;

    public async ValueTask<TransactionImportPreviewResult> Handle(PreviewTransactionImportQuery query, CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(query.CompanyId, cancellationToken)
            ?? throw new DomainException("Company not found.");

        var rawRows = ReadRawRows(query.FileContent);
        var dataRows = query.Mapping.HasHeaderRow ? rawRows.Skip(1).ToList() : rawRows;

        if (dataRows.Count > MaxRows)
        {
            throw new DomainException($"CSV file has too many rows (max {MaxRows}).");
        }

        var rate = await CurrencyConversion.ResolveRateAsync(
            exchangeRateProvider, logger, query.Currency, company.Currency, query.ExchangeRate, cancellationToken);

        var parsedRows = dataRows.Select((row, i) => ParseRow(row, i + 1, query.Mapping)).ToList();

        var validDates = parsedRows.Where(r => r.date is not null).Select(r => r.date!.Value).ToList();
        var existingByKey = new HashSet<(DateOnly, decimal, string)>();
        if (validDates.Count > 0)
        {
            var minDate = validDates.Min();
            var maxDate = validDates.Max().AddDays(1);
            var existing = await transactionRepository.GetInDateRangeAsync(query.CompanyId, minDate, maxDate, cancellationToken);
            foreach (var t in existing.Where(t => string.Equals(t.Currency, query.Currency, StringComparison.OrdinalIgnoreCase)))
            {
                existingByKey.Add((DateOnly.FromDateTime(t.TransactionDateUtc), t.Amount, t.Description.Trim().ToLowerInvariant()));
            }
        }

        var previews = parsedRows.Select(r =>
        {
            if (r.date is null || r.amount is null)
            {
                return new TransactionImportRowPreview(r.rowNumber, r.date, r.description, r.amount, TransactionCategory.Other, false, r.error);
            }

            var suggestion = TransactionCategorySuggester.Suggest(r.description);
            var isDuplicate = existingByKey.Contains((DateOnly.FromDateTime(r.date.Value), r.amount.Value, r.description.Trim().ToLowerInvariant()));

            return new TransactionImportRowPreview(r.rowNumber, r.date, r.description, r.amount, suggestion.Category, isDuplicate, null);
        }).ToList();

        var importableCount = previews.Count(r => r.ParseError is null && !r.IsDuplicate);

        return new TransactionImportPreviewResult(previews, rate, importableCount);
    }

    private static List<string[]> ReadRawRows(byte[] fileContent)
    {
        using var reader = new StringReader(Encoding.UTF8.GetString(fileContent));
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = false,
            MissingFieldFound = null,
            BadDataFound = null,
        };
        using var csv = new CsvReader(reader, config);

        var rows = new List<string[]>();
        while (csv.Read())
        {
            var count = csv.Parser.Count;
            var row = new string[count];
            for (var i = 0; i < count; i++)
            {
                row[i] = csv.GetField(i) ?? string.Empty;
            }
            rows.Add(row);
        }
        return rows;
    }

    private static (int rowNumber, DateTime? date, string description, decimal? amount, string? error) ParseRow(
        string[] row, int rowNumber, CsvColumnMapping mapping)
    {
        string Field(int index) => index >= 0 && index < row.Length ? row[index].Trim() : string.Empty;

        var description = Field(mapping.DescriptionColumnIndex);
        var dateText = Field(mapping.DateColumnIndex);
        var amountText = Field(mapping.AmountColumnIndex).Replace("$", "").Replace("R", "").Replace(",", "").Trim();

        if (!DateTime.TryParseExact(dateText, mapping.DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return (rowNumber, null, description, null, $"Couldn't parse date '{dateText}'.");
        }

        if (!decimal.TryParse(amountText, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var amount) || amount == 0)
        {
            return (rowNumber, date, description, null, $"Couldn't parse amount '{Field(mapping.AmountColumnIndex)}'.");
        }

        return (rowNumber, DateTime.SpecifyKind(date, DateTimeKind.Utc), description, amount, null);
    }
}
