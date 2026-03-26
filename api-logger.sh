#!/bin/bash

# =============================================================
#  API Logger Script
#  Calls Banking API endpoints at intervals to generate logs
#  Usage: ./api-logger.sh [interval_seconds] [duration_seconds]
#  
#  Examples:
#    ./api-logger.sh                    # Default: 5s interval, infinite
#    ./api-logger.sh 10                 # 10s interval, infinite
#    ./api-logger.sh 5 120              # 5s interval, 2 minutes total
# =============================================================

API_URL="http://localhost:5000/api/banking"
INTERVAL="${1:-5}"              # Default: 5 seconds
DURATION="${2:-0}"              # Default: infinite (0)
ELAPSED=0

# Test accounts from seed data
ACCOUNTS=("ACC-1001" "ACC-1002" "ACC-1003")

echo "🚀 Starting API Logger Script"
echo "   API URL: $API_URL"
echo "   Interval: ${INTERVAL}s"
if [ "$DURATION" -gt 0 ]; then
    echo "   Duration: ${DURATION}s"
else
    echo "   Duration: Infinite (press Ctrl+C to stop)"
fi
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

api_call() {
    local endpoint="$1"
    local method="$2"
    local data="$3"
    
    if [ "$method" = "GET" ]; then
        curl -s -X GET "$API_URL$endpoint" \
            -H "Content-Type: application/json" \
            -w "\n[%{http_code}]" 2>/dev/null
    else
        curl -s -X POST "$API_URL$endpoint" \
            -H "Content-Type: application/json" \
            -d "$data" \
            -w "\n[%{http_code}]" 2>/dev/null
    fi
}

call_random_endpoint() {
    local random=$((RANDOM % 5))
    local account1=${ACCOUNTS[$((RANDOM % ${#ACCOUNTS[@]}))]}
    local account2=${ACCOUNTS[$((RANDOM % ${#ACCOUNTS[@]}))]}
    
    case $random in
        0)
            echo "📤 GET /accounts (list all)"
            api_call "/accounts" "GET"
            ;;
        1)
            echo "🔍 GET /accounts/$account1 (get balance)"
            api_call "/accounts/$account1" "GET"
            ;;
        2)
            echo "📋 GET /transactions/$account1 (get history)"
            api_call "/transactions/$account1" "GET"
            ;;
        3)
            echo "💸 POST /transfer"
            api_call "/transfer" "POST" "{
                \"fromAccountNumber\": \"$account1\",
                \"toAccountNumber\": \"$account2\",
                \"amount\": $((RANDOM % 500 + 10))
            }"
            ;;
        4)
            echo "🔐 POST /login"
            api_call "/login" "POST" "{
                \"accountNumber\": \"$account1\",
                \"password\": \"test\"
            }"
            ;;
    esac
    echo ""
}

while true; do
    TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$TIMESTAMP] Call #$((ELAPSED / INTERVAL + 1))"
    
    call_random_endpoint
    
    if [ "$DURATION" -gt 0 ]; then
        ELAPSED=$((ELAPSED + INTERVAL))
        if [ $ELAPSED -ge $DURATION ]; then
            echo "✅ Completed. Total time: ${ELAPSED}s"
            break
        fi
    fi
    
    sleep "$INTERVAL"
done

echo "script finished."
