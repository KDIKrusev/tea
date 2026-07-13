# Stage 1: Build the Angular application
FROM node:22.9-alpine AS build

# Set working directory
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci --legacy-peer-deps

# Copy application source
COPY . .

# Build the application for production
RUN npm run build -- --configuration production

# Stage 2: Serve the application with Nginx
FROM nginx:alpine

# Copy custom nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Copy built application from build stage
COPY --from=build /app/dist/k-sail-calculator/browser /usr/share/nginx/html

# Copy Docker-specific configuration
COPY public/config.docker.json /usr/share/nginx/html/config.json

# Expose port 80
EXPOSE 80

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
