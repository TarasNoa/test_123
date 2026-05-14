FROM oven/bun:1.2-alpine AS builder
WORKDIR /app

COPY src/Frontend/package.json src/Frontend/bun.lock* ./
RUN bun install --frozen-lockfile

COPY src/Frontend/ .

RUN bun run build

FROM nginx:1.27-alpine AS runtime
COPY --from=builder /app/.output/public /usr/share/nginx/html

RUN printf 'server {\n  listen 3000;\n  root /usr/share/nginx/html;\n  index index.html;\n  location /api/ {\n    proxy_pass http://gateway:8080;\n    proxy_http_version 1.1;\n    proxy_set_header Host $host;\n    proxy_set_header X-Real-IP $remote_addr;\n    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n    proxy_set_header X-Forwarded-Proto $scheme;\n  }\n  location ~* \\.(js|css|json|woff|woff2|png|jpg|jpeg|gif|ico|svg)$ {\n    expires -1;\n    add_header Cache-Control "no-store, no-cache, must-revalidate, proxy-revalidate";\n    add_header Pragma "no-cache";\n    try_files $uri =404;\n  }\n  location / {\n    try_files $uri $uri/ /index.html;\n    add_header Cache-Control "no-store, no-cache, must-revalidate";\n  }\n}\n' \
    > /etc/nginx/conf.d/default.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
