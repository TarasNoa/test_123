# ═══════════════════════════════════════════════
#  Frontend — Multi-stage build
#  Stage 1: Build inside Linux container (avoids Windows Vinxi bugs)
#  Stage 2: Nginx serving static files
# ═══════════════════════════════════════════════

# ─── Stage 1: Builder ───
FROM node:20-alpine AS builder

WORKDIR /app

# Install dependencies with reduced parallel connections to avoid ECONNRESET
COPY src/Frontend/package.json src/Frontend/package-lock.json ./
RUN npm install --maxsockets=1 --fetch-retries=10 --fetch-retry-mintimeout=20000 --fetch-retry-maxtimeout=120000
COPY src/Frontend/ ./
RUN npm run build

# ─── Stage 2: Runtime ───
FROM nginx:1.27-alpine AS runtime

# Copy built static output
COPY --from=builder /app/.output/public /usr/share/nginx/html

# Copy nginx config (proxies /api /hubs /ws → gateway)
COPY src/Frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
