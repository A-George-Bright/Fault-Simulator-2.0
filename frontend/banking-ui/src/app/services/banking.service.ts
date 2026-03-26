// src/app/services/banking.service.ts
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Account,
  LoginRequest,
  LoginResponse,
  Transaction,
  TransferRequest,
  TransferResponse
} from '../models/banking.models';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class BankingService {

  private readonly api = environment.apiUrl;

  constructor(private http: HttpClient) { }

  // Accounts
  getAccounts(): Observable<Account[]> {
    return this.http.get<Account[]>(`${this.api}/accounts`);
  }

  getAccount(accountNumber: string): Observable<Account> {
    return this.http.get<Account>(`${this.api}/accounts/${accountNumber}`);
  }

  // Auth
  login(payload: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.api}/login`, payload);
  }

  // Transfer
  transfer(payload: TransferRequest): Observable<TransferResponse> {
    return this.http.post<TransferResponse>(`${this.api}/transfer`, payload);
  }

  // Transactions
  getTransactions(accountNumber: string): Observable<Transaction[]> {
    return this.http.get<Transaction[]>(`${this.api}/transactions/${accountNumber}`);
  }

  // Chaos
  chaosNullReference(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/null-reference`);
  }

  chaosDbTimeout(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/db-timeout`);
  }

  chaosUnhandled(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/unhandled`);
  }

    chaosSlowTransaction(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/slow-transaction`);
  }

  chaosSlowQuery(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/slow-query`);
  }

  chaosExternalApi(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/external-api`);
  }

  chaosRuntimeMetrics(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/runtime-metrics`);
  }

  chaosDeepStack(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/deep-stack`);
  }

  chaosThroughput(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/throughput`);
  }

  chaosBusinessEvent(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/business-event`);
  }

  chaosHttpRequest(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/http-request`);
  }

  chaosTransactionTrace(): Observable<unknown> {
    return this.http.get(`${this.api}/chaos/transaction-trace`);
  }
}
