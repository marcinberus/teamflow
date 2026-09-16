import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-login-page',
  imports: [MatButtonModule, MatCardModule, RouterLink],
  template: `
    <main class="auth-page">
      <mat-card class="auth-card" appearance="outlined">
        <mat-card-header>
          <mat-card-title>Sign in to TeamFlow</mat-card-title>
          <mat-card-subtitle>Authentication arrives in Phase 4.</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>This route is public and intentionally renders outside the application shell.</p>
        </mat-card-content>
        <mat-card-actions align="end">
          <a mat-button routerLink="/register">Create account</a>
          <a mat-flat-button routerLink="/projects">Preview workspace</a>
        </mat-card-actions>
      </mat-card>
    </main>
  `,
})
export class LoginPage {}
