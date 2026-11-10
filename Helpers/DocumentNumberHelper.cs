using WarehouseManagementApp.Models;

namespace WarehouseManagementApp.Helpers;

public static class DocumentNumberHelper
{
    public static string GetPrefix(DocumentType type) => type switch
    {
        DocumentType.Receipt => "REC",
        DocumentType.Expense => "EXP",
        DocumentType.WriteOff => "WRO",
        _ => "DOC"
    };

    public static string FormatDocumentNumber(DocumentType type, DateTime date, int sequenceNumber)
    {
        string prefix = GetPrefix(type);
        return $"{prefix}-{date:yyyyMMdd}-{Math.Max(1, sequenceNumber):D4}";
    }

    public static int ExtractSequenceNumber(string? documentNumber, string expectedPrefixWithDate)
    {
        if (string.IsNullOrWhiteSpace(documentNumber) || !documentNumber.StartsWith(expectedPrefixWithDate, StringComparison.OrdinalIgnoreCase))
            return 0;

        string suffix = documentNumber[expectedPrefixWithDate.Length..];
        return int.TryParse(suffix, out int seq) ? seq : 0;
    }
}

