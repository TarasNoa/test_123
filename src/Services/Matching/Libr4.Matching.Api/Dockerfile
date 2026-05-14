FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
RUN apk add --no-cache icu-libs tzdata wget
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Shared/", "src/Shared/"]
COPY ["src/Services/Matching/", "src/Services/Matching/"]
RUN dotnet restore "src/Services/Matching/Libr4.Matching.Api/Libr4.Matching.Api.csproj"
RUN dotnet publish "src/Services/Matching/Libr4.Matching.Api/Libr4.Matching.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Libr4.Matching.Api.dll"]
