#!/bin/sh
set -eu
if [ ! -s /etc/nginx/certs/localhost.crt ] || [ ! -s /etc/nginx/certs/localhost.key ] || ! openssl x509 -checkend 86400 -noout -in /etc/nginx/certs/localhost.crt; then
  mkdir -p /etc/nginx/certs
  umask 077
  openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout /etc/nginx/certs/localhost.key -out /etc/nginx/certs/localhost.crt \
    -subj '/CN=localhost' -addext 'subjectAltName=DNS:localhost,IP:127.0.0.1,IP:::1'
fi
