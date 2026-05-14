FROM rust:1.89-slim AS builder

WORKDIR /build/rust/libr4-embeddings

RUN apt-get update && apt-get install -y pkg-config libssl-dev protobuf-compiler build-essential && rm -rf /var/lib/apt/lists/*

COPY rust/libr4-embeddings/Cargo.toml rust/libr4-embeddings/Cargo.lock* ./
COPY rust/libr4-embeddings/src ./src
COPY rust/libr4-embeddings/build.rs ./
COPY rust/libr4-embeddings/proto ./proto

COPY rust/shared ../shared
RUN cargo build --release

FROM debian:trixie-slim
RUN apt-get update && apt-get install -y libssl3 ca-certificates libgomp1 libstdc++6 netcat-openbsd && rm -rf /var/lib/apt/lists/*

COPY --from=builder /build/rust/libr4-embeddings/target/release/libr4-embeddings /usr/local/bin/

EXPOSE 50061
CMD ["libr4-embeddings"]
