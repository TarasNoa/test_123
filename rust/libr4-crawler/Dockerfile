FROM rust:1.89-slim AS builder

WORKDIR /build/rust/libr4-crawler

RUN apt-get update && apt-get install -y pkg-config libssl-dev protobuf-compiler && rm -rf /var/lib/apt/lists/*

COPY rust/libr4-crawler/Cargo.toml rust/libr4-crawler/Cargo.lock* ./
COPY rust/libr4-crawler/src ./src
COPY rust/libr4-crawler/build.rs ./
COPY rust/libr4-crawler/proto ./proto

COPY rust/shared ../shared
RUN cargo build --release

FROM debian:trixie-slim
RUN apt-get update && apt-get install -y libssl3 ca-certificates netcat-openbsd && rm -rf /var/lib/apt/lists/*

COPY --from=builder /build/rust/libr4-crawler/target/release/libr4-crawler /usr/local/bin/

EXPOSE 50060
CMD ["libr4-crawler"]
