#!/bin/sh
# O volume nomeado nasce como root. Este script entrega a posse dos diretórios
# de mídia ao usuário app e só então sobe a API sem privilégio de root.
set -eu

media_root="${SocialPublishing__Media__LocalRoot:-/app/data/social-media}"
object_root="${ObjectStorage__LocalRoot:-/app/data/object-storage}"

if [ "$(id -u)" = "0" ]; then
  mkdir -p "$media_root" "$object_root"
  chown -R app:app "$media_root" "$object_root"
  export HOME=/home/app
  export USER=app
  export LOGNAME=app
  exec setpriv --reuid=app --regid=app --init-groups --inh-caps=-all -- dotnet WebApi.dll "$@"
fi

exec dotnet WebApi.dll "$@"
