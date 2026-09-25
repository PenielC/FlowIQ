namespace FlowIQ.Application.BankTransactions.TransactionImport;

public record CsvColumnMapping(
    int DateColumnIndex,
    int DescriptionColumnIndex,
    int AmountColumnIndex,
    string DateFormat,
    bool HasHeaderRow);
