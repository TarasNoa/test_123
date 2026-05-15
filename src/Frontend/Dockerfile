# ═══════════════════════════════════════════════
#  Frontend — Runtime-only (copies pre-built local output)
#  Build locally first: cd src/Frontend && npm run build
# ═══════════════════════════════════════════════

FROM nginx:1.27-alpine AS runtime

# Copy pre-built static output from local build
COPY src/Frontend/.output/public /usr/share/nginx/html

# Copy nginx config (proxies /api /hubs /ws → gateway)
COPY src/Frontend/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
