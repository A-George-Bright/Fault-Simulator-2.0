// src/app/components/dashboard/dashboard.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule }      from '@angular/common';
import { Router, RouterModule } from '@angular/router';
import { BankingService }    from '../../services/banking.service';
import { Account, Transaction } from '../../models/banking.models';
import { catchError, Observable, of }    from 'rxjs';

interface Alert {
  type: 'success' | 'warning' | 'error';
  message: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {

  account: Account | null = null;
  transactions: Transaction[] = [];
  loading        = true;
  txLoading      = true;
  chaosLoading   = '';
  alert: Alert | null = null;

  constructor(
    private svc:    BankingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const raw = sessionStorage.getItem('currentAccount');
    if (!raw) { this.router.navigate(['/login']); return; }

    this.account = JSON.parse(raw) as Account;
    this.loadFreshBalance();
    this.loadTransactions();
  }

  loadFreshBalance(): void {
    this.loading = true;
    this.svc.getAccount(this.account!.accountNumber)
      .pipe(catchError(() => of(this.account!)))
      .subscribe(acc => {
        this.account = acc;
        sessionStorage.setItem('currentAccount', JSON.stringify(acc));
        this.loading = false;
      });
  }

  loadTransactions(): void {
    this.txLoading = true;
    this.svc.getTransactions(this.account!.accountNumber)
      .pipe(catchError(() => of([] as Transaction[])))
      .subscribe(txns => {
        this.transactions = txns;
        this.txLoading    = false;
      });
  }

// ── Chaos ─────────────────────────────────────────────────
  triggerChaos(type: string): void {
    this.chaosLoading = type;
    this.alert = null;

    let obs$: Observable<unknown>;

    switch (type) {
      case 'null':        obs$ = this.svc.chaosNullReference(); break;
      case 'db':          obs$ = this.svc.chaosDbTimeout(); break;
      case 'unhandled':   obs$ = this.svc.chaosUnhandled(); break;
      case 'slow-tx':     obs$ = this.svc.chaosSlowTransaction(); break;
      case 'slow-query':  obs$ = this.svc.chaosSlowQuery(); break;
      case 'ext-api':     obs$ = this.svc.chaosExternalApi(); break;
      case 'runtime':     obs$ = this.svc.chaosRuntimeMetrics(); break;
      case 'stack':       obs$ = this.svc.chaosDeepStack(); break;
      case 'throughput':  obs$ = this.svc.chaosThroughput(); break;
      case 'business':    obs$ = this.svc.chaosBusinessEvent(); break;
      case 'http-req':    obs$ = this.svc.chaosHttpRequest(); break;
      case 'trace':       obs$ = this.svc.chaosTransactionTrace(); break;
      default: return;
    }

    obs$.pipe(
      catchError(err => {
        this.alert = {
          type: 'error',
          message: `Logged ✓ — ${err.error?.message ?? err.message ?? 'Server error generated'}`
        };
        this.chaosLoading = '';
        return of(null);
      })
    ).subscribe(res => {
      if (res !== null) {
        // Handle successful 200 OK responses that are still intentional logs
        const msg = (res as any)?.message ?? 'Log generated successfully.';
        this.alert = { type: 'success', message: `Logged ✓ — ${msg}` };
      }
      this.chaosLoading = '';
    });
  }

  // ── Helpers ───────────────────────────────────────────────
  statusClass(status: string): string {
    return {
      'SUCCESS': 'badge-success',
      'WARNING': 'badge-warn',
      'FAILED' : 'badge-error'
    }[status] ?? 'badge-dim';
  }

  dismissAlert(): void { this.alert = null; }
}
