using Npgsql;
using SDI.Back.Template.Exceptions;
using SDI.Back.Template.Models.Responses;
using System.Net;

namespace SDI.Back.Template.Middlewares;

public sealed class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            context.Response.StatusCode = ex.StatusCode;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(ex.Message, ex.StatusCode, ex.Errors));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Ja existe um registro com os dados informados.", StatusCodes.Status409Conflict));
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Um relacionamento informado nao existe no banco de dados.", StatusCodes.Status400BadRequest));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro nao tratado.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsJsonAsync(ApiResponse<object>.Fail("Erro interno ao processar a requisicao.", (int)HttpStatusCode.InternalServerError));
        }
    }
}
