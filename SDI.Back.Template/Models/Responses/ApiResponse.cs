namespace SDI.Back.Template.Models.Responses;

public sealed class ApiResponse<T>
{
    public int StatusHttp { get; init; }
    public string Mensagem { get; init; } = string.Empty;
    public T? Resultado { get; init; }
    public IReadOnlyCollection<string> Erros { get; init; } = [];

    public static ApiResponse<T> Ok(T resultado, string mensagem = "Operacao realizada com sucesso.")
    {
        return new ApiResponse<T>
        {
            StatusHttp = StatusCodes.Status200OK,
            Mensagem = mensagem,
            Resultado = resultado
        };
    }

    public static ApiResponse<T> Created(T resultado, string mensagem = "Registro criado com sucesso.")
    {
        return new ApiResponse<T>
        {
            StatusHttp = StatusCodes.Status201Created,
            Mensagem = mensagem,
            Resultado = resultado
        };
    }

    public static ApiResponse<T> Fail(string mensagem, int statusHttp, IReadOnlyCollection<string>? erros = null)
    {
        return new ApiResponse<T>
        {
            StatusHttp = statusHttp,
            Mensagem = mensagem,
            Erros = erros ?? []
        };
    }
}
