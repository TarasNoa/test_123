FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
RUN apk add --no-cache icu-libs tzdata
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Shared/", "src/Shared/"]
COPY ["src/Services/Tasks/", "src/Services/Tasks/"]
RUN dotnet restore "src/Services/Tasks/Libr4.Tasks.Api/Libr4.Tasks.Api.csproj"
RUN dotnet publish "src/Services/Tasks/Libr4.Tasks.Api/Libr4.Tasks.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Libr4.Tasks.Api.dll"]

