#!/bin/bash
# =============================================================
#  organize.sh
#  Drop this file into the same folder as all your downloaded
#  files, then run it once. It builds the full project
#  structure and moves every file into the right place.
#
#  HOW TO RUN:
#    1. Put this script in the folder with all the flat files
#    2. Open Terminal, cd into that folder:
#          cd ~/Downloads/your-folder
#    3. Make it executable and run:
#          chmod +x organize.sh && ./organize.sh
# =============================================================

# Colours
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m' # No Colour

# Root = folder where this script lives
ROOT="$(cd "$(dirname "$0")" && pwd)"

echo ""
echo -e "${CYAN}=================================================${NC}"
echo -e "${CYAN}  FaultyBankingApp – Folder Organiser (Mac)${NC}"
echo -e "${CYAN}=================================================${NC}"
echo -e "${GRAY}Working folder: $ROOT${NC}"
echo ""

# ── Helper: move a file and report ──────────────────────────
move_file() {
    local FILE="$1"
    local DEST_DIR="$ROOT/$2"

    mkdir -p "$DEST_DIR"

    if [ -f "$ROOT/$FILE" ]; then
        mv "$ROOT/$FILE" "$DEST_DIR/$FILE"
        echo -e "  ${GREEN}[OK]${NC} $FILE  →  $2/"
    else
        echo -e "  ${GRAY}[--] $FILE  (not found, skipping)${NC}"
    fi
}

# =============================================================
#  DATABASE
# =============================================================
echo -e "${YELLOW}── database/ ────────────────────────────────────${NC}"
move_file "setup.sql" "database"

# =============================================================
#  BACKEND
# =============================================================
API="backend/BankingApi"

echo ""
echo -e "${YELLOW}── backend/BankingApi/ ──────────────────────────${NC}"
move_file "BankingApi.csproj"  "$API"
move_file "Program.cs"         "$API"
move_file "appsettings.json"   "$API"
move_file "web.config"         "$API"

echo ""
echo -e "${YELLOW}── backend/BankingApi/Logging/ ──────────────────${NC}"
move_file "BankingLogger.cs"     "$API/Logging"

echo ""
echo -e "${YELLOW}── backend/BankingApi/Controllers/ ──────────────${NC}"
move_file "BankingController.cs" "$API/Controllers"

echo ""
echo -e "${YELLOW}── backend/BankingApi/Data/ ─────────────────────${NC}"
move_file "BankingDbContext.cs"  "$API/Data"

echo ""
echo -e "${YELLOW}── backend/BankingApi/Models/ ───────────────────${NC}"
move_file "Account.cs"     "$API/Models"
move_file "Transaction.cs" "$API/Models"
move_file "Dtos.cs"        "$API/Models"

# =============================================================
#  FRONTEND
# =============================================================
UI="frontend/banking-ui"
SRC="$UI/src"
APP="$SRC/app"

echo ""
echo -e "${YELLOW}── frontend/banking-ui/ ─────────────────────────${NC}"
move_file "package.json"      "$UI"
move_file "angular.json"      "$UI"
move_file "tsconfig.json"     "$UI"
move_file "tsconfig.app.json" "$UI"

echo ""
echo -e "${YELLOW}── frontend/banking-ui/src/ ─────────────────────${NC}"
move_file "index.html"  "$SRC"
move_file "main.ts"     "$SRC"
move_file "styles.scss" "$SRC"

echo ""
echo -e "${YELLOW}── .../src/app/  (shell component) ──────────────${NC}"
move_file "app.component.ts"   "$APP"
move_file "app.component.html" "$APP"
move_file "app.component.scss" "$APP"
move_file "app.config.ts"      "$APP"
move_file "app.routes.ts"      "$APP"

echo ""
echo -e "${YELLOW}── .../app/models/ ──────────────────────────────${NC}"
move_file "banking.models.ts"  "$APP/models"

echo ""
echo -e "${YELLOW}── .../app/services/ ────────────────────────────${NC}"
move_file "banking.service.ts" "$APP/services"

echo ""
echo -e "${YELLOW}── .../app/components/login/ ────────────────────${NC}"
move_file "login.component.ts"   "$APP/components/login"
move_file "login.component.html" "$APP/components/login"
move_file "login.component.scss" "$APP/components/login"

echo ""
echo -e "${YELLOW}── .../app/components/dashboard/ ────────────────${NC}"
move_file "dashboard.component.ts"   "$APP/components/dashboard"
move_file "dashboard.component.html" "$APP/components/dashboard"
move_file "dashboard.component.scss" "$APP/components/dashboard"

echo ""
echo -e "${YELLOW}── .../app/components/transfer/ ─────────────────${NC}"
move_file "transfer.component.ts"   "$APP/components/transfer"
move_file "transfer.component.html" "$APP/components/transfer"
move_file "transfer.component.scss" "$APP/components/transfer"

echo ""
echo -e "${GRAY}  [--] README.md stays at root (no move needed)${NC}"

# =============================================================
#  Print final tree
# =============================================================
echo ""
echo -e "${CYAN}=================================================${NC}"
echo -e "${CYAN}  Done! Final structure:${NC}"
echo -e "${CYAN}=================================================${NC}"
echo ""

# Use find to print the tree (works on all Macs without 'tree' installed)
find "$ROOT" \
    ! -name "organize.sh" \
    ! -name ".DS_Store" \
    | sort \
    | while read -r ITEM; do
        REL="${ITEM#$ROOT/}"
        DEPTH=$(echo "$REL" | tr -cd '/' | wc -c)
        PAD=$(printf '%*s' $((DEPTH * 2)) '')
        NAME=$(basename "$ITEM")
        if [ -d "$ITEM" ]; then
            echo -e "${PAD}📁 ${YELLOW}${NAME}${NC}"
        else
            echo "${PAD}📄 ${NAME}"
        fi
    done

echo ""
echo -e "${CYAN}=================================================${NC}"
echo -e "${CYAN}  Next steps:${NC}"
echo ""
echo -e "  1. ${GREEN}Open in VS Code:${NC}"
echo -e "     ${GRAY}code .${NC}"
echo ""
echo -e "  2. ${GREEN}Edit your MySQL password:${NC}"
echo -e "     ${GRAY}backend/BankingApi/appsettings.json${NC}"
echo -e "     ${GRAY}→ replace YOUR_MYSQL_PASSWORD${NC}"
echo ""
echo -e "  3. ${GREEN}Run the database script:${NC}"
echo -e "     ${GRAY}mysql -u root -p < database/setup.sql${NC}"
echo ""
echo -e "  4. ${GREEN}Start the .NET API (Terminal 1):${NC}"
echo -e "     ${GRAY}cd backend/BankingApi${NC}"
echo -e "     ${GRAY}dotnet run${NC}"
echo ""
echo -e "  5. ${GREEN}Start Angular (Terminal 2):${NC}"
echo -e "     ${GRAY}cd frontend/banking-ui${NC}"
echo -e "     ${GRAY}npm install${NC}"
echo -e "     ${GRAY}ng serve${NC}"
echo ""
echo -e "${CYAN}=================================================${NC}"
echo ""
