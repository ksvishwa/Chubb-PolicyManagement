import { Component } from '@angular/core';
import { RouterOutlet, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ThemeService } from './core/services/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, CommonModule],
  template: `
    <header class="app-header" role="banner">
      <nav role="navigation" aria-label="Main navigation">
        <a routerLink="/" class="logo" aria-label="Chubb Policy Dashboard home">
          Chubb APAC Policy Management
        </a>
        <ul class="nav-links">
          <li><a routerLink="/policies" routerLinkActive="active">Policies</a></li>
          <li><a routerLink="/summary" routerLinkActive="active">Summary</a></li>
        </ul>
      </nav>
      <button
        (click)="themeService.toggleTheme()"
        class="theme-toggle"
        [attr.aria-label]="'Switch to ' + (themeService.theme() === 'light' ? 'dark' : 'light') + ' mode'"
      >
        {{ themeService.theme() === 'light' ? '🌙' : '☀️' }}
      </button>
    </header>
    <main class="app-main">
      <router-outlet />
    </main>
  `,
})
export class AppComponent {
  constructor(readonly themeService: ThemeService) {
    themeService.applyTheme(themeService.theme());
  }
}
