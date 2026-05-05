# Libr4 Python Development Environment
# Includes: Python 3.11, Conda, Jupyter, common ML/Data Science libraries

FROM python:3.11-slim

LABEL maintainer="libr4@example.com"
LABEL description="Libr4 Python Development Environment"

# Install system dependencies
RUN apt-get update && apt-get install -y \
    build-essential \
    curl \
    git \
    wget \
    libpq-dev \
    libssl-dev \
    libffi-dev \
    python3-dev \
    && rm -rf /var/lib/apt/lists/*

# Install Miniconda
ENV CONDA_DIR=/opt/conda
RUN wget --quiet https://repo.anaconda.com/miniconda/Miniconda3-latest-Linux-x86_64.sh -O /tmp/miniconda.sh \
    && /bin/bash /tmp/miniconda.sh -b -p ${CONDA_DIR} \
    && rm /tmp/miniconda.sh

ENV PATH=${CONDA_DIR}/bin:${PATH}

# Create base conda environment with common packages
RUN conda create -n libr4 python=3.11 -y \
    && conda run -n libr4 pip install --upgrade pip

# Install common Python packages in conda environment
COPY requirements.txt /tmp/requirements.txt
RUN conda run -n libr4 pip install -r /tmp/requirements.txt \
    || echo "Some packages may have failed, continuing..."

# Install Jupyter
RUN conda run -n libr4 pip install \
    jupyter \
    jupyterlab \
    ipykernel \
    && conda run -n libr4 python -m ipykernel install --name libr4 --display-name "Python 3.11 (Libr4)"

# Install Node.js (for hybrid projects)
RUN apt-get update && apt-get install -y \
    curl \
    && curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Install additional tools
RUN apt-get update && apt-get install -y \
    tree \
    htop \
    procps \
    vim \
    nano \
    && rm -rf /var/lib/apt/lists/*

# Setup workspace directory
WORKDIR /workspace

# Create non-root user
RUN useradd -m -s /bin/bash developer \
    && chown -R developer:developer /workspace

# Set conda environment as default
ENV CONDA_DEFAULT_ENV=libr4
ENV PATH=${CONDA_DIR}/envs/libr4/bin:${PATH}

# Keep container alive by default
CMD ["sleep", "infinity"]
