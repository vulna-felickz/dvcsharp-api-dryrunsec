#!/bin/bash

echo "Restoring NuGet packages..."
dotnet restore

echo "Updating database..."
dotnet ef database update

echo "Starting application..."
dotnet watch run

