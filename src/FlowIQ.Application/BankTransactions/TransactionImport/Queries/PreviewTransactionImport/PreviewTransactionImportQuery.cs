using Mediator;

namespace FlowIQ.Application.BankTransactions.TransactionImport.Queries.PreviewTransactionImport;

public record PreviewTransactionImportQuery(
    Guid CompanyId,
    byte[] FileContent,
    CsvColumnMapping Mapping,
    string Currency,
    decimal? ExchangeRate) : IQuery<TransactionImportPreviewResult>;

public record TransactionImportPreviewResult(
    IReadOnlyCollection<TransactionImportRowPreview> Rows,
    decimal ExchangeRate,
    int ImportableCount);
