# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS build
WORKDIR /src

# Vite inlines VITE_* env vars at build time, so they must be present when
# `vite build` runs — runtime env vars never reach the bundle. Railway
# auto-passes any service variable as --build-arg if the Dockerfile declares
# it with ARG. The ENV lines re-export them so vite picks them up.
ARG VITE_AUTH0_DOMAIN
ARG VITE_AUTH0_CLIENT_ID
ARG VITE_AUTH0_AUDIENCE
ENV VITE_AUTH0_DOMAIN=$VITE_AUTH0_DOMAIN \
    VITE_AUTH0_CLIENT_ID=$VITE_AUTH0_CLIENT_ID \
    VITE_AUTH0_AUDIENCE=$VITE_AUTH0_AUDIENCE

COPY package.json package-lock.json* ./
RUN npm ci

COPY . .
RUN npm run build

FROM nginx:alpine AS runtime

# Drop the default config; we provide our own as a template that envsubst rewrites
# at container start so Railway's $PORT and $API_PORT can be injected per env.
RUN rm /etc/nginx/conf.d/default.conf

COPY nginx.conf /etc/nginx/templates/default.conf.template
COPY --from=build /src/dist /usr/share/nginx/html

COPY docker-entrypoint.sh /usr/local/bin/start-nginx.sh
RUN chmod +x /usr/local/bin/start-nginx.sh

ENV PORT=8080 \
    API_PORT=8080 \
    API_HOST=api.railway.internal

EXPOSE 8080

# Override nginx:alpine's ENTRYPOINT (which has its own template processor that
# would substitute env vars before we've computed NAMESERVER from resolv.conf).
ENTRYPOINT []
CMD ["/usr/local/bin/start-nginx.sh"]
