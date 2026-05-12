FROM rust:1.80-slim AS builder

WORKDIR /build

RUN apt-get update && apt-get install -y pkg-config libssl-dev && rm -rf /var/lib/apt/lists/*

COPY Cargo.toml Cargo.lock* ./
COPY src ./src
COPY build.rs ./
COPY proto ./proto

COPY ../shared /build-shared
RUN cargo build --release

FROM debian:bookworm-slim
RUN apt-get update && apt-get install -y libssl3 ca-certificates && rm -rf /var/lib/apt/lists/*

COPY --from=builder /build/target/release/libr4-embeddings /usr/local/bin/

EXPOSE 50061
CMD ["libr4-embeddings"]
