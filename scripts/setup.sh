#!/bin/bash

echo "🚀 Setting up NutrientInsight Engine..."

# Check prerequisites
echo "Checking prerequisites..."

if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET 8 SDK not found. Please install from: https://dotnet.microsoft.com/download"
    exit 1
fi

if ! command -v node &> /dev/null; then
    echo "❌ Node.js not found. Please install from: https://nodejs.org/"
    exit 1
fi

echo "✅ Prerequisites OK"

# Backend setup
echo ""
echo "📦 Installing backend dependencies..."
cd backend
dotnet restore
if [ $? -ne 0 ]; then
    echo "❌ Backend restore failed"
    exit 1
fi
echo "✅ Backend dependencies installed"

# Frontend setup
echo ""
echo "📦 Installing frontend dependencies..."
cd ../frontend
npm install
if [ $? -ne 0 ]; then
    echo "❌ Frontend install failed"
    exit 1
fi
echo "✅ Frontend dependencies installed"

echo ""
echo "✅ Setup complete!"
echo ""
echo "To run the application, execute: ./scripts/run.sh"
