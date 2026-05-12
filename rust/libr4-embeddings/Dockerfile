FROM rust:1.80-slim AS builder

WORKDIR /build

RUN apt-get update && apt-get install -y cmake pkg-config libssl-dev curl && rm -rf /var/lib/apt/lists/*

ENV ORT_VERSION=1.19.2
RUN curl -fsSL "https://github.com/microsoft/onnxruntime/releases/download/v${ORT_VERSION}/onnxruntime-linux-x64-${ORT_VERSION}.tgz" \
    -o ort.tgz && tar xzf ort.tgz && rm ort.tgz

ENV ORT_LIB_LOCATION=/build/onnxruntime-linux-x64-${ORT_VERSION}

COPY Cargo.toml Cargo.lock* ./
COPY src ./src
COPY build.rs ./
COPY proto ./proto

# Workspace shared
COPY ../shared /build-shared
RUN cargo build --release

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y libssl3 ca-certificates && rm -rf /var/lib/apt/lists/*

COPY --from=builder /build/target/release/libr4-embeddings /usr/local/bin/
COPY --from=builder /build/onnxruntime-linux-x64-*/lib/*.so* /usr/local/lib/

RUN ldconfig

EXPOSE 50061
CMD ["libr4-embeddings"]
