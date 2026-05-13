namespace Libr4.Social.Api.Endpoints;

public static class RouteHandlerBuilderExtensions
{
    public static RouteHandlerBuilder WithCacheableResponse(this RouteHandlerBuilder builder, TimeSpan duration)
    {
        return builder;
    }

    public static RouteHandlerBuilder InvalidatesCache(this RouteHandlerBuilder builder, string pattern)
    {
        return builder;
    }
}
