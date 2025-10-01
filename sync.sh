#!/bin/bash

# Sync script to transfer SideSnap project files to Windows
# Excludes build artifacts and unnecessary files

SOURCE_DIR="/root/projects/SideLaunch"
DEST_DIR="/mnt/c/Users/%USERPROFILE%/Desktop/Projects/SideSnap"

echo "Starting sync from WSL to Windows..."
echo "Source: $SOURCE_DIR"
echo "Destination: $DEST_DIR"
echo ""

# Create destination directory if it doesn't exist
mkdir -p "$DEST_DIR"

# Sync files using rsync with exclusions
rsync -av --progress \
  --exclude 'bin/' \
  --exclude 'obj/' \
  --exclude '.vs/' \
  --exclude '.vscode/' \
  --exclude '*.user' \
  --exclude '*.suo' \
  --exclude '.git/' \
  --exclude 'node_modules/' \
  --exclude 'packages/' \
  --exclude '*.cache' \
  --exclude 'TestResults/' \
  --exclude '.idea/' \
  "$SOURCE_DIR/" "$DEST_DIR/"

echo ""
echo "Sync completed successfully!"
echo "Files transferred to: $DEST_DIR"