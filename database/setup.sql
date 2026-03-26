-- =============================================================
--  FaultyBankingApp – MySQL Setup
--  Run once:  mysql -u root -p < database/setup.sql
-- =============================================================

CREATE DATABASE IF NOT EXISTS FaultyBankingDB
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE FaultyBankingDB;

-- -------------------------------------------------------------
-- Accounts
-- -------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Accounts (
    Id            INT           NOT NULL AUTO_INCREMENT,
    AccountNumber VARCHAR(20)   NOT NULL,
    OwnerName     VARCHAR(100)  NOT NULL,
    Balance       DECIMAL(18,2) NOT NULL DEFAULT 0.00,
    CreatedAt     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    UNIQUE KEY uq_account_number (AccountNumber)
);

-- -------------------------------------------------------------
-- Transactions
-- -------------------------------------------------------------
CREATE TABLE IF NOT EXISTS Transactions (
    Id              INT           NOT NULL AUTO_INCREMENT,
    FromAccountId   INT           NULL,
    ToAccountId     INT           NULL,
    Amount          DECIMAL(18,2) NOT NULL,
    TransactionType VARCHAR(50)   NOT NULL,   -- TRANSFER | DEPOSIT | WITHDRAWAL
    Status          VARCHAR(20)   NOT NULL,   -- SUCCESS  | FAILED  | WARNING
    Notes           VARCHAR(255)  NULL,
    CreatedAt       DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (Id),
    FOREIGN KEY (FromAccountId) REFERENCES Accounts(Id) ON DELETE SET NULL,
    FOREIGN KEY (ToAccountId)   REFERENCES Accounts(Id) ON DELETE SET NULL
);

-- -------------------------------------------------------------
-- Seed Data
-- ACC-1001  Alice   $5,000   → normal transfers        → INFO logs
-- ACC-1002  Bob     $150     → insufficient funds      → WARNING logs
-- ACC-1003  Charlie $25,000  → transfer destination
-- -------------------------------------------------------------
INSERT INTO Accounts (AccountNumber, OwnerName, Balance) VALUES
    ('ACC-1001', 'Alice Johnson',  5000.00),
    ('ACC-1002', 'Bob Smith',       150.00),
    ('ACC-1003', 'Charlie Brown', 25000.00);

SELECT * FROM Accounts;
