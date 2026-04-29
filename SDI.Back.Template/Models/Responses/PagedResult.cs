namespace SDI.Back.Template.Models.Responses;

public sealed class PagedResult<T>
{
    public IReadOnlyCollection<T> Itens { get; init; } = [];
    public int Pagina { get; init; }
    public int TamanhoPagina { get; init; }
    public long Total { get; init; }
}
