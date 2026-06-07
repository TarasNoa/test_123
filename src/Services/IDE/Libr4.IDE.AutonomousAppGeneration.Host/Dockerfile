FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet publish "src/Services/IDE/Libr4.IDE.AutonomousAppGeneration.Host/Libr4.IDE.AutonomousAppGeneration.Host.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app
EXPOSE 8080
RUN apk add --no-cache icu-libs tzdata docker-cli
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Libr4.IDE.AutonomousAppGeneration.Host.dll"]
