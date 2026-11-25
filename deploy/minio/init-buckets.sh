#!/bin/sh
set -eu

ENDPOINT="${MINIO_ENDPOINT:-http://minio:9000}"
ALIAS="${MINIO_MC_ALIAS:-admetricspro}"
ROOT_USER="${MINIO_ROOT_USER:?MINIO_ROOT_USER is required}"
ROOT_PASSWORD="${MINIO_ROOT_PASSWORD:?MINIO_ROOT_PASSWORD is required}"
ORGANIC_BUCKET="${MINIO_ORGANIC_BUCKET:-admetricspro-organic}"
REPORTS_BUCKET="${MINIO_REPORTS_BUCKET:-admetricspro-reports}"
BRANDING_BUCKET="${MINIO_BRANDING_BUCKET:-admetricspro-branding}"
STAGING_PREFIX="${MINIO_STAGING_PREFIX:-organic-media/staging/}"
STAGING_EXPIRY_DAYS="${MINIO_STAGING_EXPIRY_DAYS:-1}"

mc alias set "$ALIAS" "$ENDPOINT" "$ROOT_USER" "$ROOT_PASSWORD"

mc mb --ignore-existing "$ALIAS/$ORGANIC_BUCKET"
mc mb --ignore-existing "$ALIAS/$REPORTS_BUCKET"
mc mb --ignore-existing "$ALIAS/$BRANDING_BUCKET"

mc anonymous set none "$ALIAS/$ORGANIC_BUCKET"
mc anonymous set none "$ALIAS/$REPORTS_BUCKET"
mc anonymous set none "$ALIAS/$BRANDING_BUCKET"

if mc ilm rule ls "$ALIAS/$ORGANIC_BUCKET" 2>/dev/null | grep -Fq "$STAGING_PREFIX"; then
  echo "ILM staging rule already present on $ORGANIC_BUCKET"
else
  mc ilm rule add \
    --expire-days "$STAGING_EXPIRY_DAYS" \
    --prefix "$STAGING_PREFIX" \
    "$ALIAS/$ORGANIC_BUCKET"
  echo "ILM staging rule added on $ORGANIC_BUCKET prefix $STAGING_PREFIX"
fi

echo "MinIO buckets initialized."