#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Variables
REPO_URL="https://github.com/slekrem/OrderBookFetcher.git"
INSTALL_DIR="/home/$(whoami)/OrderBookFetcher"
SERVICE_NAME="orderbookfetcher.service"

# Function to check exit code
check_status() {
    if [ $? -ne 0 ]; then
        echo -e "${RED}Error during $1. Aborting.${NC}"
        exit 1
    fi
}

echo -e "${GREEN}### Starting OrderBookFetcher Installation ###${NC}"

# 1. Update system and install dependencies
echo "Updating system and installing dependencies..."
sudo apt-get update && sudo apt-get upgrade -y
check_status "System update"

# Add Microsoft package source for .NET SDK
echo "Adding Microsoft package source for .NET SDK 9.0..."
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb
check_status "Adding Microsoft package source"

sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0 git sqlite3
check_status "Installation of .NET 9.0, Git, and SQLite"

# Check .NET version
echo "Installed .NET version:"
dotnet --version
check_status "Checking .NET version"

# 2. Clone repository
echo "Cloning repository from $REPO_URL..."
mkdir -p "$INSTALL_DIR"
cd "$INSTALL_DIR"
git clone "$REPO_URL" .
check_status "Git clone"

# 3. Build project
echo "Building the project..."
dotnet restore
check_status "dotnet restore"

dotnet publish -c Release -o ./publish
check_status "dotnet publish"

# 4. Set up Systemd service
echo "Setting up Systemd service..."
cat <<EOF | sudo tee /etc/systemd/system/$SERVICE_NAME
[Unit]
Description=OrderBookFetcher Service
After=network.target

[Service]
ExecStart=/usr/bin/dotnet $INSTALL_DIR/publish/OrderBookFetcher.dll
WorkingDirectory=$INSTALL_DIR/publish
Restart=always
RestartSec=10
User=$(whoami)
Environment=ASPNETCORE_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
EOF
check_status "Creating Systemd service"

# Reload Systemd and start service
sudo systemctl daemon-reload
check_status "systemctl daemon-reload"

sudo systemctl enable $SERVICE_NAME
check_status "systemctl enable"

sudo systemctl start $SERVICE_NAME
check_status "systemctl start"

# 5. Check status
echo "Checking service status..."
sleep 2 # Wait briefly for the service to start
sudo systemctl status $SERVICE_NAME --no-pager

echo -e "${GREEN}### Installation completed successfully! ###${NC}"
echo "Manage the service with the following commands:"
echo "  Start: sudo systemctl start $SERVICE_NAME"
echo "  Stop: sudo systemctl stop $SERVICE_NAME"
echo "  Restart: sudo systemctl restart $SERVICE_NAME"
echo "  Real-time logs: journalctl -u $SERVICE_NAME -f"
echo "Database: $INSTALL_DIR/publish/orderbook.db"
echo "Error logs: $INSTALL_DIR/publish/logs/error.log"
