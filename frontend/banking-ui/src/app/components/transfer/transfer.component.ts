// src/app/components/transfer/transfer.component.ts
import { Component, OnInit } from '@angular/core';
import { CommonModule }      from '@angular/common';
import { FormsModule }       from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { BankingService }    from '../../services/banking.service';
import { Account }           from '../../models/banking.models';
import { catchError, of }    from 'rxjs';

interface TransferAlert {
  type: 'success' | 'warning' | 'error';
  message: string;
  logLevel: string;
}

@Component({
  selector: 'app-transfer',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './transfer.component.html',
  styleUrls: ['./transfer.component.scss']
})
export class TransferComponent implements OnInit {

  currentAccount: Account | null = null;
  allAccounts: Account[] = [];

  fromAccountNumber = '';
  toAccountNumber   = '';
  amount: number | null = null;

  loading  = false;
  alert: TransferAlert | null = null;

  constructor(
    private svc:    BankingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const raw = sessionStorage.getItem('currentAccount');
    if (!raw) { this.router.navigate(['/login']); return; }

    this.currentAccount   = JSON.parse(raw) as Account;
    this.fromAccountNumber = this.currentAccount.accountNumber;

    this.svc.getAccounts()
      .pipe(catchError(() => of([] as Account[])))
      .subscribe(accounts => {
        this.allAccounts = accounts.filter(
          a => a.accountNumber !== this.fromAccountNumber
        );
      });
  }

  onTransfer(): void {
    this.alert = null;

    if (!this.toAccountNumber || !this.amount || this.amount <= 0) {
      this.alert = { type: 'warning', message: 'Please fill in all fields with valid values.', logLevel: 'Warning' };
      return;
    }

    this.loading = true;

    this.svc.transfer({
      fromAccountNumber: this.fromAccountNumber,
      toAccountNumber:   this.toAccountNumber,
      amount:            this.amount
    }).pipe(
      catchError(err => {
        const body = err.error ?? { success: false, message: err.message, logLevel: 'Error' };
        return of(body);
      })
    ).subscribe(res => {
      this.loading = false;
      const level = (res.logLevel ?? 'Info').toLowerCase() as 'success' | 'warning' | 'error';

      this.alert = {
        type:     res.success ? 'success' : (level === 'error' ? 'error' : 'warning'),
        message:  res.message,
        logLevel: res.logLevel ?? 'Info'
      };

      if (res.success) {
        // Refresh session balance
        this.svc.getAccount(this.fromAccountNumber)
          .pipe(catchError(() => of(this.currentAccount!)))
          .subscribe(acc => {
            this.currentAccount = acc;
            sessionStorage.setItem('currentAccount', JSON.stringify(acc));
          });
        this.amount = null;
        this.toAccountNumber = '';
      }
    });
  }

  dismissAlert(): void { this.alert = null; }

  get transferDisabled(): boolean {
    return this.loading || !this.toAccountNumber || !this.amount || this.amount <= 0;
  }
}
