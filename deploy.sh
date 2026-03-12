#!/bin/bash
# ═══════════════════════════════════════════════════════════════════════════
#  TenXConvo — Build & Deploy Script
#
#  Usage:
#    chmod +x deploy.sh
#    ./deploy.sh                    → build everything
#    ./deploy.sh --backend-only     → build backend only
#    ./deploy.sh --frontend-only    → build frontend only
#
#  Before running:
#    1. Fill in appsettings.Production.json (copy from .example)
#    2. Create .env.production in each frontend portal
#    3. Ensure Node.js 22+ and .NET 10 SDK are installed
# ═══════════════════════════════════════════════════════════════════════════

set -e  # Exit on any error

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}  TenXConvo Build & Deploy                             ${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo ""

# ── PRE-FLIGHT CHECKS ────────────────────────────────────────────────────
echo -e "${YELLOW}Running pre-flight checks...${NC}"

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗ .NET SDK not found. Install from https://dotnet.microsoft.com${NC}"
    exit 1
fi
echo -e "${GREEN}✓ .NET SDK: $(dotnet --version)${NC}"

# Check Node.js
if ! command -v node &> /dev/null; then
    echo -e "${RED}✗ Node.js not found. Install from https://nodejs.org${NC}"
    exit 1
fi
echo -e "${GREEN}✓ Node.js: $(node --version)${NC}"

# Check production config
if [ ! -f "TenXConvo_v2/src/TenXConvo.API/appsettings.Production.json" ]; then
    echo -e "${YELLOW}⚠ appsettings.Production.json not found${NC}"
    echo -e "  Copy the example: cp TenXConvo_v2/src/TenXConvo.API/appsettings.Production.example.json TenXConvo_v2/src/TenXConvo.API/appsettings.Production.json"
    echo -e "  Then fill in your real credentials."
    echo ""
fi

echo ""

# ── BUILD BACKEND ────────────────────────────────────────────────────────
if [ "$1" != "--frontend-only" ]; then
    echo -e "${BLUE}── Building Backend (.NET API) ──────────────────────${NC}"
    cd TenXConvo_v2
    dotnet publish src/TenXConvo.API -c Release -o ./publish --nologo
    echo -e "${GREEN}✓ Backend built → TenXConvo_v2/publish/${NC}"
    cd ..
    echo ""
fi

# ── BUILD FRONTEND ───────────────────────────────────────────────────────
if [ "$1" != "--backend-only" ]; then
    echo -e "${BLUE}── Building Frontend (3 React Portals) ─────────────${NC}"

    for portal in admin-portal consultant-portal user-portal; do
        echo -e "${YELLOW}  Building $portal...${NC}"
        cd tenx-frontend/$portal

        # Check .env.production
        if [ ! -f ".env.production" ]; then
            echo -e "${RED}  ✗ .env.production not found in $portal${NC}"
            echo -e "    Create it with: echo 'VITE_API_URL=https://api.yourdomain.com' > .env.production"
            cd ../..
            continue
        fi

        npm install --silent 2>/dev/null
        npm run build --silent 2>/dev/null
        echo -e "${GREEN}  ✓ $portal built → tenx-frontend/$portal/dist/${NC}"
        cd ../..
    done
    echo ""
fi

# ── SUMMARY ──────────────────────────────────────────────────────────────
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}  Build Complete!${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo ""
echo "  Deployable outputs:"
echo "    Backend:    TenXConvo_v2/publish/"
echo "    Admin:      tenx-frontend/admin-portal/dist/"
echo "    Consultant: tenx-frontend/consultant-portal/dist/"
echo "    User:       tenx-frontend/user-portal/dist/"
echo ""
echo "  Next steps:"
echo "    1. Copy these folders to your server"
echo "    2. Set ASPNETCORE_ENVIRONMENT=Production"
echo "    3. Run: dotnet TenXConvo.API.dll"
echo "    4. Configure Nginx (see DOMAIN_CONFIGURATION_GUIDE.md)"
echo ""
echo "  Or with Docker:"
echo "    docker-compose up -d"
echo ""
