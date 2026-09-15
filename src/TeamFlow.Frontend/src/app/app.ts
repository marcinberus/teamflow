import { Component } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { RouterOutlet } from '@angular/router';
import { API_BASE_URL } from './core/config/api.config';

@Component({
  imports: [MatCardModule, RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly apiBaseUrl = API_BASE_URL;
}
