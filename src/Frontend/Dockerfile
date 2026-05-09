FROM oven/bun:1.2-alpine AS builder
WORKDIR /app

COPY src/Frontend/package.json src/Frontend/bun.lockb* ./
RUN bun install --frozen-lockfile

COPY src/Frontend/ .

ARG VITE_API_BASE_URL=http://localhost:5000
ARG VITE_WS_BASE_URL=ws://localhost:5000
ARG VITE_GRPC_BASE_URL=http://localhost:50051
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_WS_BASE_URL=$VITE_WS_BASE_URL
ENV VITE_GRPC_BASE_URL=$VITE_GRPC_BASE_URL

RUN bun run build

FROM nginx:1.27-alpine AS runtime
COPY --from=builder /app/.output/public /usr/share/nginx/html

RUN printf 'server {\n  listen 3000;\n  root /usr/share/nginx/html;\n  index index.html;\n  location / {\n    try_files $uri $uri/ /index.html;\n  }\n}\n' \
    > /etc/nginx/conf.d/default.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
