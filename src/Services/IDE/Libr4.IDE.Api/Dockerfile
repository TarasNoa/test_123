FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
RUN apk add --no-cache icu-libs tzdata netcat-openbsd wget bash
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM base AS final
WORKDIR /app
COPY publish/ide-api/ .
ENTRYPOINT ["dotnet", "Libr4.IDE.Api.dll"]
