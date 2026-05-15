# ═══════════════════════════════════════════════
#  Frontend — Multi-stage Docker build
#  Build context: repo root (docker-compose context: .)
# ═══════════════════════════════════════════════

# ═══ STAGE 1 — Build ═══
FROM node:20-alpine AS builder
WORKDIR /app

# 1. Dependencies (cache layer)
COPY src/Frontend/package*.json ./
RUN npm ci

# 2. Source & build
COPY src/Frontend/ ./
RUN npm run build

# ═══ STAGE 2 — Runtime ═══
FROM nginx:1.27-alpine AS runtime

# Copy built static output
COPY --from=builder /app/.output/public /usr/share/nginx/html

# Copy nginx config (proxies /api /hubs /ws → gateway)
COPY src/Frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
