/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,
  experimental: {},
  async rewrites() {
    const api = process.env.NEXT_PUBLIC_API_BASE_URL ?? 'http://localhost:5000'
    // P2-4 of audit roadmap: AutonomousAppGeneration host runs as a separate
    // service on its own port (default 5200). Route /api/ide/* to it; the
    // gateway catch-all keeps owning the rest of /api/* and /hubs/*.
    const autoGen =
      process.env.NEXT_PUBLIC_AUTOGEN_BASE_URL ?? 'http://localhost:5200'
    return [
      { source: '/api/ide/:path*', destination: `${autoGen}/api/ide/:path*` },
      { source: '/api/:path*', destination: `${api}/api/:path*` },
      { source: '/hubs/:path*', destination: `${api}/hubs/:path*` },
    ]
  },
}
export default nextConfig
