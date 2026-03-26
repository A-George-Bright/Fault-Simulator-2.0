// src/app/app.component.ts
import { Component }        from '@angular/core';
import { CommonModule }     from '@angular/common';
import { RouterModule }     from '@angular/router';
import { Router }           from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {

  constructor(private router: Router) {}

  get isLoggedIn(): boolean {
    return !!sessionStorage.getItem('currentAccount');
  }

  get currentUser(): string {
    const raw = sessionStorage.getItem('currentAccount');
    if (!raw) return '';
    try { return JSON.parse(raw).ownerName; } catch { return ''; }
  }

  get currentRoute(): string {
    return this.router.url;
  }

  logout(): void {
    sessionStorage.removeItem('currentAccount');
    this.router.navigate(['/login']);
  }
}
