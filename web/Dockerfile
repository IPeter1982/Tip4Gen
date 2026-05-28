# syntax=docker/dockerfile:1.7

FROM node:22-alpine AS build
WORKDIR /src

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

ENV PORT=8080 \
    API_PORT=8080 \
    API_HOST=api.railway.internal

EXPOSE 8080

CMD ["sh", "-c", "envsubst '$PORT $API_HOST $API_PORT' < /etc/nginx/templates/default.conf.template > /etc/nginx/conf.d/default.conf && nginx -g 'daemon off;'"]
