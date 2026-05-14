FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 8080
RUN apk add --no-cache icu-libs tzdata docker-cli netcat-openbsd wget
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

RUN apt-get update && apt-get install -y protobuf-compiler libprotobuf-dev && rm -rf /var/lib/apt/lists/*
ENV PROTOBUF_PROTOC=/usr/bin/protoc

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]

COPY ["src/Shared/", "src/Shared/"]
COPY ["src/Services/AI/", "src/Services/AI/"]
COPY ["src/Services/IDE/", "src/Services/IDE/"]

RUN dotnet restore "src/Services/IDE/Libr4.IDE.Api/Libr4.IDE.Api.csproj"
RUN dotnet publish "src/Services/IDE/Libr4.IDE.Api/Libr4.IDE.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Libr4.IDE.Api.dll"]
