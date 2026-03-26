// src/app/components/login/login.component.ts
import { Component }    from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule }  from '@angular/forms';
import { Router }       from '@angular/router';
import { BankingService } from '../../services/banking.service';
import { catchError, of } from 'rxjs';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {

  accountNumber = '';
  password      = '';
  loading       = false;
  errorMsg      = '';

  constructor(
    private svc:    BankingService,
    private router: Router
  ) {}

  onLogin(): void {
    this.errorMsg = '';
    if (!this.accountNumber || !this.password) {
      this.errorMsg = 'Please enter your account number and password.';
      return;
    }

    this.loading = true;

    this.svc.login({ accountNumber: this.accountNumber, password: this.password })
      .pipe(
        catchError(err => {
          const msg = err.error?.message ?? 'Login failed. Please try again.';
          return of({ success: false, message: msg, account: null });
        })
      )
      .subscribe(res => {
        this.loading = false;
        if (res.success && res.account) {
          sessionStorage.setItem('currentAccount', JSON.stringify(res.account));
          this.router.navigate(['/dashboard']);
        } else {
          this.errorMsg = res.message;
        }
      });
  }
}
