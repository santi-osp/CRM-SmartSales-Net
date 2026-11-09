FROM node:24.22.1-bookworm-slim@sha256:be8204d4bd327363357f532187b752ff62759c31bedf98e9e18fc08c9cbd605c AS build
WORKDIR /web
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginxinc/nginx-unprivileged:1.29.4-alpine-slim@sha256:93a0458087e8dd06e7e59ceaeb3c54c9e73f64730282ab9387c3fe47a42bfc99 AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /web/dist/SmartSalesWeb/browser/ /usr/share/nginx/html/
EXPOSE 8080
