#!/bin/bash

# Configuration
REPO_DIR="$HOME/.openclaw/workspace"
DATE=$(date +"%Y-%m-%d %H:%M:%S")

# Navigate to workspace
cd "$REPO_DIR" || exit 1

# Check for changes
if [[ -z $(git status -s) ]]; then
  echo "[$DATE] No changes to sync."
  exit 0
fi

# Add changes
git add .

# Commit changes
git commit -m "chore(auto): sync workspace $DATE"

# Push changes
git push origin main

if [ $? -eq 0 ]; then
  echo "[$DATE] Sync successful."
else
  echo "[$DATE] Sync failed."
  exit 1
fi
