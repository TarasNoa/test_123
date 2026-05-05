# Libr4 .NET Development Environment
# Includes: .NET 8 SDK, Node.js, Python (for hybrid projects)

FROM mcr.microsoft.com/dotnet/sdk:8.0

LABEL maintainer="libr4@example.com"
LABEL description="Libr4 .NET Development Environment"

# Install Node.js (for frontend builds)
RUN apt-get update && apt-get install -y \
    curl \
    gnupg \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Install Python (for hybrid projects, ML, scripts)
RUN apt-get update && apt-get install -y \
    python3 \
    python3-pip \
    python3-venv \
    && rm -rf /var/lib/apt/lists/*

# Install additional tools
RUN apt-get update && apt-get install -y \
    git \
    jq \
    tree \
    htop \
    procps \
    && rm -rf /var/lib/apt/lists/*

# Install global .NET tools
RUN dotnet tool install --global dotnet-ef \
    && dotnet tool install --global dotnet-format \
    && dotnet tool install --global dotnet-outdated-tool

# Add .NET tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Install common npm packages globally
RUN npm install -g \
    @angular/cli \
    @vue/cli \
    create-react-app \
    typescript \
    ts-node

# Setup workspace directory
WORKDIR /workspace

# Create non-root user for security (optional, can be overridden)
RUN useradd -m -s /bin/bash developer \
    && chown -R developer:developer /workspace

# Pre-warm NuGet cache with common packages
COPY --chown=developer:developer warmup/ ./warmup/
RUN if [ -f warmup/restore.csproj ]; then \
    cd warmup && dotnet restore && cd .. && rm -rf warmup; \
    fi

# Keep container alive by default
CMD ["sleep", "infinity"]
