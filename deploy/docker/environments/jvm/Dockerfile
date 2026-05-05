# Libr4 JVM Development Environment
# Includes: OpenJDK 21, Kotlin, Gradle, Maven, Scala (optional)

FROM eclipse-temurin:21-jdk-jammy

LABEL maintainer="libr4@example.com"
LABEL description="Libr4 JVM Development Environment"

# Install basic tools
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    git \
    unzip \
    zip \
    tree \
    htop \
    procps \
    && rm -rf /var/lib/apt/lists/*

# Install Gradle
ENV GRADLE_VERSION=8.5
RUN wget https://services.gradle.org/distributions/gradle-${GRADLE_VERSION}-bin.zip -O /tmp/gradle.zip \
    && unzip /tmp/gradle.zip -d /opt \
    && rm /tmp/gradle.zip
ENV GRADLE_HOME=/opt/gradle-${GRADLE_VERSION}
ENV PATH=${GRADLE_HOME}/bin:${PATH}

# Install Maven
ENV MAVEN_VERSION=3.9.6
RUN wget https://dlcdn.apache.org/maven/maven-3/${MAVEN_VERSION}/binaries/apache-maven-${MAVEN_VERSION}-bin.tar.gz -O /tmp/maven.tar.gz \
    && tar -xzf /tmp/maven.tar.gz -C /opt \
    && rm /tmp/maven.tar.gz
ENV MAVEN_HOME=/opt/apache-maven-${MAVEN_VERSION}
ENV PATH=${MAVEN_HOME}/bin:${PATH}

# Install Kotlin
ENV KOTLIN_VERSION=1.9.22
RUN wget https://github.com/JetBrains/kotlin/releases/download/v${KOTLIN_VERSION}/kotlin-compiler-${KOTLIN_VERSION}.zip -O /tmp/kotlin.zip \
    && unzip /tmp/kotlin.zip -d /opt \
    && rm /tmp/kotlin.zip
ENV KOTLIN_HOME=/opt/kotlinc
ENV PATH=${KOTLIN_HOME}/bin:${PATH}

# Install Scala (optional but useful)
ENV SCALA_VERSION=3.3.1
RUN wget https://github.com/lampepfl/dotty/releases/download/${SCALA_VERSION}/scala3-${SCALA_VERSION}.zip -O /tmp/scala.zip \
    && unzip /tmp/scala.zip -d /opt \
    && rm /tmp/scala.zip
ENV SCALA_HOME=/opt/scala3-${SCALA_VERSION}
ENV PATH=${SCALA_HOME}/bin:${PATH}

# Install SBT
RUN apt-get update && apt-get install -y apt-transport-https \
    && echo "deb https://repo.scala-sbt.org/scalasbt/debian all main" | tee /etc/apt/sources.list.d/sbt.list \
    && curl -sL "https://keyserver.ubuntu.com/pks/lookup?op=get&search=0x2EE0EA64E40A89B84B2DF73499E82A75642AC823" | apt-key add \
    && apt-get update \
    && apt-get install -y sbt \
    && rm -rf /var/lib/apt/lists/*

# Install Node.js (for frontend builds)
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
    && apt-get install -y nodejs \
    && rm -rf /var/lib/apt/lists/*

# Pre-download common dependencies
RUN mkdir -p /root/.gradle && \
    mkdir -p /root/.m2

# Setup workspace directory
WORKDIR /workspace

# Create non-root user
RUN useradd -m -s /bin/bash developer \
    && chown -R developer:developer /workspace

# Keep container alive by default
CMD ["sleep", "infinity"]
