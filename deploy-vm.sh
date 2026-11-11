#!/bin/bash

###############################################################################
# Port Logistics - VM Deployment Script
# This script deploys the application to a DEI VM environment
###############################################################################

set -e  # Exit on any error

# Configuration
APP_NAME="portlogistics"
DEPLOY_DIR="/var/www/$APP_NAME"
BACKUP_DIR="/var/backups/$APP_NAME"
SERVICE_NAME="portlogistics.service"
TEMP_DEPLOY="/tmp/portlogistics-deploy"

echo "================================================"
echo "Port Logistics - VM Deployment"
echo "================================================"
echo ""

# Create directories if they don't exist
echo "[INFO] Setting up directories..."
sudo mkdir -p "$DEPLOY_DIR"
sudo mkdir -p "$BACKUP_DIR"
sudo mkdir -p "/var/log/$APP_NAME"

# Backup current deployment (if exists)
if [ -d "$DEPLOY_DIR/current" ]; then
    echo "[INFO] Creating backup of current deployment..."
    BACKUP_NAME="backup-$(date +%Y%m%d-%H%M%S)"
    sudo cp -r "$DEPLOY_DIR/current" "$BACKUP_DIR/$BACKUP_NAME"
    echo "[SUCCESS] Backup created: $BACKUP_DIR/$BACKUP_NAME"
fi

# Stop the service if running
echo "[INFO] Stopping service..."
if sudo systemctl is-active --quiet "$SERVICE_NAME"; then
    sudo systemctl stop "$SERVICE_NAME"
    echo "[SUCCESS] Service stopped"
else
    echo "[INFO] Service is not running"
fi

# Extract new deployment
echo "[INFO] Extracting deployment package..."
sudo rm -rf "$DEPLOY_DIR/next"
sudo mkdir -p "$DEPLOY_DIR/next"
sudo tar -xzf "$TEMP_DEPLOY/deploy-package.tar.gz" -C "$DEPLOY_DIR/next"
echo "[SUCCESS] Package extracted"

# Copy configuration files (if they exist in current)
if [ -f "$DEPLOY_DIR/current/appsettings.Production.json" ]; then
    echo "[INFO] Preserving production configuration..."
    sudo cp "$DEPLOY_DIR/current/appsettings.Production.json" "$DEPLOY_DIR/next/"
fi

# Switch deployments
echo "[INFO] Switching to new deployment..."
sudo rm -rf "$DEPLOY_DIR/old"
if [ -d "$DEPLOY_DIR/current" ]; then
    sudo mv "$DEPLOY_DIR/current" "$DEPLOY_DIR/old"
fi
sudo mv "$DEPLOY_DIR/next" "$DEPLOY_DIR/current"

# Set permissions
echo "[INFO] Setting permissions..."
sudo chown -R www-data:www-data "$DEPLOY_DIR/current"
sudo chmod -R 755 "$DEPLOY_DIR/current"
sudo chmod +x "$DEPLOY_DIR/current/WebApi" || true

# Start the service
echo "[INFO] Starting service..."
sudo systemctl start "$SERVICE_NAME"
sleep 2

# Check if service is running
if sudo systemctl is-active --quiet "$SERVICE_NAME"; then
    echo "[SUCCESS] Service started successfully"
    sudo systemctl status "$SERVICE_NAME" --no-pager -l
else
    echo "[ERROR] Service failed to start"
    echo "[INFO] Checking logs..."
    sudo journalctl -u "$SERVICE_NAME" -n 50 --no-pager
    
    # Rollback if old deployment exists
    if [ -d "$DEPLOY_DIR/old" ]; then
        echo "[INFO] Rolling back to previous deployment..."
        sudo rm -rf "$DEPLOY_DIR/current"
        sudo mv "$DEPLOY_DIR/old" "$DEPLOY_DIR/current"
        sudo systemctl start "$SERVICE_NAME"
        echo "[WARNING] Rolled back to previous deployment"
    fi
    exit 1
fi

# Cleanup
echo "[INFO] Cleaning up..."
sudo rm -rf "$DEPLOY_DIR/old"
rm -rf "$TEMP_DEPLOY"

# Keep only last 5 backups
echo "[INFO] Cleaning old backups..."
cd "$BACKUP_DIR"
ls -t | tail -n +6 | xargs -r sudo rm -rf

echo ""
echo "================================================"
echo "[SUCCESS] Deployment completed successfully!"
echo "================================================"
echo ""
echo "Application is running at: http://localhost:5000"
echo "Check logs with: sudo journalctl -u $SERVICE_NAME -f"
echo ""
