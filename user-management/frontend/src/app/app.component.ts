import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent {
  apiBase = 'http://localhost:5000';
  token = localStorage.getItem('token') ?? '';

  loginModel = { userName: '', password: '' };
  passwordModel = { currentPassword: '', newPassword: '' };

  me: { userName: string; role: string; createdAt: string } | null = null;
  users: Array<{ userName: string; role: string }> = [];
  message = '';

  constructor(private readonly http: HttpClient) {
    this.loadProfile();
  }

  login(): void {
    this.http.post<{ token: string }> (`${this.apiBase}/api/auth/login`, this.loginModel).subscribe({
      next: (response) => {
        this.token = response.token;
        localStorage.setItem('token', this.token);
        this.message = 'Login successful.';
        this.loadProfile();
      },
      error: () => (this.message = 'Login failed.')
    });
  }

  loginWithSso(): void {
    window.location.href = `${this.apiBase}/api/auth/sso`;
  }

  handleSsoToken(): void {
    const url = new URL(window.location.href);
    const ssoToken = url.searchParams.get('token');
    if (!ssoToken) {
      return;
    }

    this.token = ssoToken;
    localStorage.setItem('token', ssoToken);
    window.history.replaceState({}, document.title, window.location.pathname);
    this.loadProfile();
  }

  changePassword(): void {
    this.http.post(`${this.apiBase}/api/users/change-password`, this.passwordModel, { headers: this.authHeaders() }).subscribe({
      next: () => (this.message = 'Password changed successfully.'),
      error: () => (this.message = 'Password change failed.')
    });
  }

  loadProfile(): void {
    this.handleSsoToken();
    if (!this.token) {
      return;
    }

    this.http.get<{ userName: string; role: string; createdAt: string }>(`${this.apiBase}/api/users/me`, { headers: this.authHeaders() }).subscribe({
      next: (profile) => {
        this.me = profile;
        if (profile.role === 'Admin') {
          this.loadUsers();
        }
      },
      error: () => {
        this.me = null;
        this.token = '';
        localStorage.removeItem('token');
      }
    });
  }

  private loadUsers(): void {
    this.http.get<Array<{ userName: string; role: string }>>(`${this.apiBase}/api/admin/users`, { headers: this.authHeaders() }).subscribe({
      next: (users) => (this.users = users),
      error: () => (this.users = [])
    });
  }

  logout(): void {
    this.token = '';
    this.me = null;
    this.users = [];
    localStorage.removeItem('token');
  }

  private authHeaders(): HttpHeaders {
    return new HttpHeaders({ Authorization: `Bearer ${this.token}` });
  }
}
