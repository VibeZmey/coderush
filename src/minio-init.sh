#!/bin/sh
set -eu

case "$MINIO_PROFILE_BUCKET" in
  ''|*[!a-z0-9.-]*) echo "Invalid profile bucket name" >&2; exit 1 ;;
esac

if [ "$MINIO_PROFILE_ACCESS_KEY" = "$MINIO_ROOT_USER" ]; then
  echo "Profile media must use a dedicated MinIO account" >&2
  exit 1
fi

mc alias set storage http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"
mc mb --ignore-existing "storage/$MINIO_PROFILE_BUCKET"
mc anonymous set none "storage/$MINIO_PROFILE_BUCKET"

cat > /tmp/profile-media-policy.json <<EOF
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": ["s3:ListBucket"],
      "Resource": ["arn:aws:s3:::$MINIO_PROFILE_BUCKET"]
    },
    {
      "Effect": "Allow",
      "Action": ["s3:GetObject", "s3:PutObject"],
      "Resource": ["arn:aws:s3:::$MINIO_PROFILE_BUCKET/profiles/*"]
    }
  ]
}
EOF

mc admin policy create storage coderush-profile-media /tmp/profile-media-policy.json
mc admin user add storage "$MINIO_PROFILE_ACCESS_KEY" "$MINIO_PROFILE_SECRET_KEY"
mc admin policy attach storage coderush-profile-media --user "$MINIO_PROFILE_ACCESS_KEY"
