FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Services/Matching/Libr4.Matching.Api/Libr4.Matching.Api.csproj", "Services/Matching/Libr4.Matching.Api/"]
COPY ["src/Services/Matching/Libr4.Matching.Application/Libr4.Matching.Application.csproj", "Services/Matching/Libr4.Matching.Application/"]
COPY ["src/Services/Matching/Libr4.Matching.Domain/Libr4.Matching.Domain.csproj", "Services/Matching/Libr4.Matching.Domain/"]
COPY ["src/Services/Matching/Libr4.Matching.Domain.Algorithms.FSharp/Libr4.Matching.Domain.Algorithms.FSharp.fsproj", "Services/Matching/Libr4.Matching.Domain.Algorithms.FSharp/"]
COPY ["src/Services/Matching/Libr4.Matching.Infrastructure/Libr4.Matching.Infrastructure.csproj", "Services/Matching/Libr4.Matching.Infrastructure/"]
COPY ["src/Shared/Libr4.Shared.Kernel/Libr4.Shared.Kernel.csproj", "Shared/Libr4.Shared.Kernel/"]
COPY ["src/Shared/Libr4.Shared.Infrastructure/Libr4.Shared.Infrastructure.csproj", "Shared/Libr4.Shared.Infrastructure/"]
COPY ["src/Shared/Libr4.Shared.Web/Libr4.Shared.Web.csproj", "Shared/Libr4.Shared.Web/"]

RUN dotnet restore "Services/Matching/Libr4.Matching.Api/Libr4.Matching.Api.csproj"

COPY src/ .

RUN dotnet build "Services/Matching/Libr4.Matching.Api/Libr4.Matching.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Matching/Libr4.Matching.Api/Libr4.Matching.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Libr4.Matching.Api.dll"]
