import { Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-register-page',
  imports: [MatButtonModule, MatCardModule, RouterLink],
  template: `
    <main class="auth-page">
      <mat-card class="auth-card" appearance="outlined">
        <mat-card-header>
          <mat-card-title>Create your TeamFlow account</mat-card-title>
          <mat-card-subtitle>Registration arrives in Phase 4.</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <p>This public route shares the auth area, not the protected application shell.</p>
        </mat-card-content>
        <mat-card-actions align="end">
          <a mat-button routerLink="/login">Back to sign in</a>
        </mat-card-actions>
      </mat-card>
    </main>
  `,
})
export class RegisterPage {}
