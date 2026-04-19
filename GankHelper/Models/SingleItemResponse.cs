namespace GankHelper.Models;

internal sealed class SingleItemResponse<T>
{
    public T Data { get; set; } = default!;
}
