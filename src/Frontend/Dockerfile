# Build locally first: cd src/Frontend && npm run build
# Then build this image which just serves the static output
FROM nginx:1.27-alpine AS runtime
COPY src/Frontend/.output/public /usr/share/nginx/html

RUN printf 'server {\n  listen 3000;\n  root /usr/share/nginx/html;\n  index index.html;\n  location /api/ {\n    proxy_pass http://host.docker.internal:5000;\n    proxy_http_version 1.1;\n    proxy_set_header Host $host;\n    proxy_set_header X-Real-IP $remote_addr;\n    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n    proxy_set_header X-Forwarded-Proto $scheme;\n  }\n  location /hubs/ {\n    proxy_pass http://host.docker.internal:5000;\n    proxy_http_version 1.1;\n    proxy_set_header Upgrade $http_upgrade;\n    proxy_set_header Connection "upgrade";\n    proxy_set_header Host $host;\n    proxy_set_header X-Real-IP $remote_addr;\n    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n    proxy_set_header X-Forwarded-Proto $scheme;\n    proxy_read_timeout 86400;\n  }\n  location /ws/ {\n    proxy_pass http://host.docker.internal:5000;\n    proxy_http_version 1.1;\n    proxy_set_header Upgrade $http_upgrade;\n    proxy_set_header Connection "upgrade";\n    proxy_set_header Host $host;\n    proxy_set_header X-Real-IP $remote_addr;\n    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n    proxy_set_header X-Forwarded-Proto $scheme;\n    proxy_read_timeout 86400;\n  }\n  location ~* \\.(js|css|json|woff|woff2|png|jpg|jpeg|gif|ico|svg)$ {\n    expires -1;\n    add_header Cache-Control "no-store, no-cache, must-revalidate, proxy-revalidate";\n    add_header Pragma "no-cache";\n    try_files $uri =404;\n  }\n  location / {\n    try_files $uri $uri/ /index.html;\n    add_header Cache-Control "no-store, no-cache, must-revalidate";\n  }\n}\n' \
    > /etc/nginx/conf.d/default.conf

EXPOSE 3000
CMD ["nginx", "-g", "daemon off;"]
