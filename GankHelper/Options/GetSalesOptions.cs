using System.Diagnostics.CodeAnalysis;

namespace GankHelper.Options;

internal sealed class GetSalesOptions
{
    
    [MemberNotNullWhen(true, nameof(CsvFilePath))]
    public bool ShouldWriteToCsvFile { get; set; }
    
    public string? CsvFilePath { get; set; }

    public int PageSize { get; set; } = 500;
}
