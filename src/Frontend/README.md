# Libr4 Frontend - Golden Stack (2026)

Modern frontend implementation using cutting-edge 2026 technologies.

## Tech Stack

### Core Framework & Runtime
- **Framework**: SolidStart (SolidJS) - Zero overhead, instant reactivity
- **Runtime**: Bun - Fastest JS runtime, written in Zig
- **Build Tool**: Rolldown (native Rust bundler) - Instant build and HMR

### Intelligence & Real-time Layer
- **AI SDK**: TanStack AI (Alpha 2026) - Vendor-neutral, extreme type safety
- **Data Sync**: TanStack DB - Reactive client storage with differential dataflow
- **Communication**: gRPC-web - Direct Protobuf from C# to TypeScript

### Rendering & Performance
- **Styling**: Tailwind CSS 4.0 (Lightning CSS) - 20-30x faster processing
- **Shared Logic**: Rust WASM Core - Heavy operations in browser
- **Validation**: ArkType - World's fastest JS type validator

### UI System
- **Components**: shadcn/ui (Radix Primitives) - Full code ownership
- **Animations**: Motion One - Web Animations API, GPU-accelerated

### Tooling
- **Linter/Formatter**: Biome (Rust)
- **Package Manager**: Bun
- **Icons**: Lucide Solid (SVG-based, tree-shakable)

## Getting Started

```bash
# Install dependencies
bun install

# Start development server
bun run dev

# Build for production
bun run build

# Lint code
bun run lint

# Type check
bun run typecheck
```

## Architecture

```
Frontend (SolidJS + Bun)
    ↓ gRPC-web
Backend (C# + F# + Rust)
    ↓
PostgreSQL
```

## Features

- **IDE Interface**: Code editor with Rust sandbox execution
- **Real-time Sync**: Agent state synchronization via TanStack DB
- **Type-Safe Communication**: gRPC-web with Protobuf contracts
- **AI Integration**: TanStack AI for agent orchestration

## Configuration

Edit `src/app/routes/settings.tsx` to configure:
- gRPC endpoint (default: `http://localhost:50051`)
- AI provider and model
- Theme preferences

## Development

The frontend communicates with the backend via gRPC-web, using the same Protobuf contract defined in `src/Services/IDE/Libr4.IDE.Infrastructure/Protos/sandbox.proto`.

## License

MIT
