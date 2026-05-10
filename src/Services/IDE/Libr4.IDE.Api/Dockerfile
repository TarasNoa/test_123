FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Install protobuf compiler and tools for gRPC code generation
RUN apt-get update && apt-get install -y \
    protobuf-compiler \
    libprotobuf-dev \
    && rm -rf /var/lib/apt/lists/*

COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["global.json", "./"]

COPY ["src/Shared/", "src/Shared/"]
COPY ["src/Services/AI/", "src/Services/AI/"]
COPY ["src/Services/IDE/", "src/Services/IDE/"]

# Set environment for protobuf tools
ENV PROTOBUF_PROTOC=/usr/bin/protoc

RUN dotnet restore "src/Services/IDE/Libr4.IDE.Api/Libr4.IDE.Api.csproj"

RUN dotnet publish "src/Services/IDE/Libr4.IDE.Api/Libr4.IDE.Api.csproj" \
    -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Libr4.IDE.Api.dll"]
