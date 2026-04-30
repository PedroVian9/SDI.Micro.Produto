using SDI.Back.Template.Exceptions;

namespace SDI.Back.Template.Services;

internal static class ServiceValidation
{
    public static (int Pagina, int TamanhoPagina) NormalizePagination(int pagina, int tamanhoPagina)
    {
        return (Math.Max(1, pagina), Math.Clamp(tamanhoPagina, 1, 100));
    }

    public static string Required(string? value, string fieldName, int maxLength)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new DomainException($"{fieldName} e obrigatorio.");
        }

        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} deve ter no maximo {maxLength} caracteres.");
        }

        return normalized;
    }

    public static Guid RequiredGuid(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName} e obrigatorio.");
        }

        return value;
    }

    public static string? Optional(string? value, string fieldName, int maxLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > maxLength)
        {
            throw new DomainException($"{fieldName} deve ter no maximo {maxLength} caracteres.");
        }

        return normalized;
    }
}
