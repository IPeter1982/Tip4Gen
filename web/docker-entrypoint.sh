#!/bin/sh
set -e

# Pull the system DNS server out of /etc/resolv.conf so nginx's resolver can
# look up Railway's IPv6-only private hostnames (*.railway.internal) at
# request time. nginx wraps IPv6 resolver addresses in brackets.
NS=$(awk '/^nameserver/ {print $2; exit}' /etc/resolv.conf)
case "$NS" in
  *:*) export NAMESERVER="[$NS]" ;;
  *)   export NAMESERVER="$NS" ;;
esac

envsubst '$PORT $API_HOST $API_PORT $NAMESERVER' \
  < /etc/nginx/templates/default.conf.template \
  > /etc/nginx/conf.d/default.conf

exec nginx -g 'daemon off;'
