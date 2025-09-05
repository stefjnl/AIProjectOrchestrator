FROM nginx:latest

# Copy HTML/CSS/JS files to nginx document root
COPY . /usr/share/nginx/html/

# Custom nginx configuration for SPA routing
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]