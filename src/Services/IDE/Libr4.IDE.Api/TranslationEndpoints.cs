namespace Libr4.IDE.Api;

public static class TranslationEndpoints
{
    public static void MapTranslationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ide/translation")
            .RequireAuthorization();

        group.MapGet("/status", () => Results.Ok(new { status = "ok", endpoint = "Translation" }));
    }
}
