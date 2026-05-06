FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/IDE/Libr4.IDE.Api/Libr4.IDE.Api.csproj", "src/Services/IDE/Libr4.IDE.Api/"]
RUN dotnet restore "src/Services/IDE/Libr4.IDE.Api/Libr4.IDE.Api.csproj"
COPY . .
WORKDIR "/src/src/Services/IDE/Libr4.IDE.Api"
RUN dotnet build "Libr4.IDE.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Libr4.IDE.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Libr4.IDE.Api.dll"]
