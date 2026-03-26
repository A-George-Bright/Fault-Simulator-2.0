# FaultyBankingApp
### Full-Stack Structured Log Generator  ·  Angular + .NET 8 + MySQL + IIS

---

## Folder Structure

```
FaultyBankingApp/
│
├── database/
│   └── setup.sql                         ← Step 1: Create DB + seed data
│
├── backend/
│   └── BankingApi/
│       ├── BankingApi.csproj
│       ├── Program.cs                    ← Serilog JSON file sink config
│       ├── appsettings.json              ← DB connection string (edit this)
│       ├── appsettings.Development.json
│       ├── web.config                    ← IIS deployment config
│       │
│       ├── Logging/
│       │   └── BankingLogger.cs          ← Stamps LogLevel + ClassName + ErrorDesc
│       │
│       ├── Controllers/
│       │   └── BankingController.cs      ← All endpoints + 3 chaos fault generators
│       │
│       ├── Data/
│       │   └── BankingDbContext.cs       ← EF Core MySQL context
│       │
│       └── Models/
│           ├── Account.cs
│           ├── Transaction.cs
│           └── Dtos.cs                   ← Request/Response shapes
│
└── frontend/
    └── banking-ui/
        ├── package.json
        ├── angular.json
        ├── tsconfig.json
        ├── tsconfig.app.json
        │
        └── src/
            ├── index.html
            ├── main.ts
            ├── styles.scss               ← Global CSS variables (dark theme)
            │
            └── app/
                ├── app.component.ts      ← Shell + navbar
                ├── app.component.html
                ├── app.component.scss
                ├── app.config.ts         ← Providers (HttpClient, Router)
                ├── app.routes.ts         ← Lazy-loaded routes
                │
                ├── models/
                │   └── banking.models.ts ← Shared TypeScript interfaces
                │
                ├── services/
                │   └── banking.service.ts ← All HTTP calls to the API
                │
                └── components/
                    ├── login/            ← /login route
                    │   ├── login.component.ts
                    │   ├── login.component.html
                    │   └── login.component.scss
                    │
                    ├── dashboard/        ← /dashboard route
                    │   ├── dashboard.component.ts
                    │   ├── dashboard.component.html
                    │   └── dashboard.component.scss
                    │
                    └── transfer/         ← /transfer route
                        ├── transfer.component.ts
                        ├── transfer.component.html
                        └── transfer.component.scss
```

---

## Step-by-Step Setup

### Step 1 – MySQL

```bash
mysql -u root -p < database/setup.sql
```

Creates `FaultyBankingDB` with three seed accounts:

| Account  | Owner         | Balance   | Designed to trigger |
|----------|---------------|-----------|---------------------|
| ACC-1001 | Alice Johnson | $5,000    | INFO logs           |
| ACC-1002 | Bob Smith     | $150      | WARNING logs        |
| ACC-1003 | Charlie Brown | $25,000   | INFO logs           |

**Demo password for all accounts: `password123`**

---

### Step 2 – Backend (.NET 8)

#### 2a. Edit the connection string
Open `backend/BankingApi/appsettings.json` and replace `YOUR_MYSQL_PASSWORD`.

#### 2b. Create the log directory
```powershell
# Create folder
New-Item -ItemType Directory -Path "C:\Logs" -Force

# Grant write access for IIS worker
icacls "C:\Logs" /grant "IIS_IUSRS:(OI)(CI)F"
```

#### 2c. Run locally
```bash
cd backend/BankingApi
dotnet restore
dotnet run
```
- API: `http://localhost:5000`
- Swagger UI: `http://localhost:5000/swagger`

#### 2d. Publish to IIS
```powershell
dotnet publish -c Release -o C:\inetpub\wwwroot\BankingApi
```

**IIS Manager steps:**
1. Add new Site → Physical Path: `C:\inetpub\wwwroot\BankingApi`
2. Application Pool → **.NET CLR = No Managed Code**
3. The `web.config` is already included — no edits needed

---

### Step 3 – Frontend (Angular 17)

```bash
cd frontend/banking-ui
npm install
ng serve
```
- Dev server: `http://localhost:4200`

**For production build:**
```bash
ng build --configuration=production
# Output: dist/banking-ui/ → serve from IIS as a static site
```

> If deploying the Angular app to IIS, add a `web.config` inside `dist/banking-ui/`
> with URL rewrite rules to redirect all paths to `index.html`.

---

## Log File

**Location:** `C:\Logs\banking-app-logs.json`  
**Format:** One JSON object per line (JSON Lines), rotated daily.

### Sample log entries

**INFO – Successful login:**
```json
{
  "Timestamp": "2024-03-15T10:23:41.123Z",
  "LogLevel": "Info",
  "ClassName": "BankingController",
  "ErrorDesc": "Login successful. AccountNumber='ACC-1001', Owner='Alice Johnson', Timestamp=2024-03-15T10:23:41Z.",
  "Level": "Information"
}
```

**WARNING – Insufficient funds:**
```json
{
  "Timestamp": "2024-03-15T10:24:15.987Z",
  "LogLevel": "Warning",
  "ClassName": "BankingController",
  "ErrorDesc": "Insufficient funds. AccountNumber='ACC-1002', Owner='Bob Smith', AvailableBalance=$150.00, RequestedAmount=$500.00. Transfer blocked.",
  "Level": "Warning"
}
```

**ERROR – NullReferenceException:**
```json
{
  "Timestamp": "2024-03-15T10:25:03.456Z",
  "LogLevel": "Error",
  "ClassName": "BankingController",
  "ErrorDesc": "CHAOS: NullReferenceException in payment processing pipeline. The payment object was null – possible race condition or missing initialisation.",
  "Level": "Error",
  "Exception": "System.NullReferenceException: Object reference not set to an instance of an object..."
}
```

---

## API Endpoints

| Method | Endpoint                           | Triggers     | Log Level |
|--------|------------------------------------|--------------|-----------|
| GET    | /api/banking/accounts              | List all     | INFO      |
| GET    | /api/banking/accounts/{number}     | Balance      | INFO      |
| GET    | /api/banking/transactions/{number} | History      | INFO      |
| POST   | /api/banking/login                 | Good creds   | INFO      |
| POST   | /api/banking/login                 | Bad creds    | WARNING   |
| POST   | /api/banking/transfer              | OK transfer  | INFO      |
| POST   | /api/banking/transfer              | No funds     | WARNING   |
| GET    | /api/banking/chaos/null-reference  | NullRef      | ERROR     |
| GET    | /api/banking/chaos/db-timeout      | DB timeout   | ERROR     |
| GET    | /api/banking/chaos/unhandled       | Unhandled 500| ERROR     |

---

## Site24x7 Configuration

| Setting       | Value                                  |
|---------------|----------------------------------------|
| Log file path | `C:\Logs\banking-app-logs*.json`       |
| Log type      | JSON                                   |
| Key fields    | `LogLevel`, `ErrorDesc`, `ClassName`, `Timestamp` |
| Alert rule    | `LogLevel = "Error"`, count > 0 in 5 min |
