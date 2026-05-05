// DELETED: OptimizedContainerPool removed per Golden Stack architecture
// Container pooling logic moved to Rust: obscura/crates/container-runtime/src/container_pool.rs
// Rust handles pool management, maintenance, and pre-warming
// C# uses thin gRPC client via ContainerRuntimeGrpcClient.cs
// PoolConfig, PoolStats, PooledContainer types are now defined in ContainerRuntimeGrpcClient.cs and ContainerLifecycleBridge.cs
