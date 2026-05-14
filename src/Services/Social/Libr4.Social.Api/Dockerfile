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
COPY ["src/Services/Social/", "src/Services/Social/"]
RUN dotnet restore "src/Services/Social/Libr4.Social.Api/Libr4.Social.Api.csproj"
RUN dotnet publish "src/Services/Social/Libr4.Social.Api/Libr4.Social.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Libr4.Social.Api.dll"]
