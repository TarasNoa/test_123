FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Libr4.AI.Api/Libr4.AI.Api.csproj", "Libr4.AI.Api/"]
COPY ["Libr4.AI.Application/Libr4.AI.Application.csproj", "Libr4.AI.Application/"]
COPY ["Libr4.AI.Domain/Libr4.AI.Domain.csproj", "Libr4.AI.Domain/"]
COPY ["Libr4.AI.Infrastructure/Libr4.AI.Infrastructure.csproj", "Libr4.AI.Infrastructure/"]
RUN dotnet restore "Libr4.AI.Api/Libr4.AI.Api.csproj"
COPY . .
WORKDIR "/src/Libr4.AI.Api"
RUN dotnet build "Libr4.AI.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Libr4.AI.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Libr4.AI.Api.dll"]

