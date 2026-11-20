# Stage 1: build the React/Vite application with Node
FROM node:20-alpine AS build
WORKDIR /app

COPY package*.json ./
RUN npm ci

COPY . .

ARG VITE_API_BASE_URL
ARG VITE_BASIC_AUTH_USERNAME
ARG VITE_BASIC_AUTH_PASSWORD
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ENV VITE_BASIC_AUTH_USERNAME=${VITE_BASIC_AUTH_USERNAME}
ENV VITE_BASIC_AUTH_PASSWORD=${VITE_BASIC_AUTH_PASSWORD}
RUN npm run build

# Stage 2: serve the static files with nginx
FROM nginx:alpine AS final
COPY --from=build /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
