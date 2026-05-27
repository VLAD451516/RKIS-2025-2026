#!/bin/bash

# Exit on error
set -e

echo "Starting Restaurant System..."

# Build the solution
echo "Building solution..."
dotnet build RestaurantSystem.slnx

# Set ports
export ASPNETCORE_URLS="http://localhost:5000"
export BLAZOR_URLS="http://localhost:5001"

# Run API in background
echo "Starting API on http://localhost:5000 ..."
cd RestaurantSystem.API
dotnet run --no-build --urls=http://localhost:5000 > ../api.log 2>&1 &
API_PID=$!

# Run Client
echo "Starting Client on http://localhost:5001 ..."
cd ../RestaurantSystem.Client
dotnet run --no-build --urls=http://localhost:5001

# Cleanup on exit
kill $API_PID
