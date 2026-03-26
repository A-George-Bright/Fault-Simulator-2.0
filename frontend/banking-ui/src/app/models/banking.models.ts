// src/app/models/banking.models.ts

export interface Account {
  id: number;
  accountNumber: string;
  ownerName: string;
  balance: number;
}

export interface Transaction {
  id: number;
  fromAccount: string | null;
  toAccount: string | null;
  amount: number;
  transactionType: string;
  status: string;
  notes: string | null;
  createdAt: string;
}

export interface LoginRequest {
  accountNumber: string;
  password: string;
}

export interface LoginResponse {
  success: boolean;
  message: string;
  account: Account | null;
}

export interface TransferRequest {
  fromAccountNumber: string;
  toAccountNumber: string;
  amount: number;
}

export interface TransferResponse {
  success: boolean;
  message: string;
  logLevel: string;
}
