import { Injectable, signal } from '@angular/core';

export type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'chubb-theme';
  readonly theme = signal<Theme>(this.getInitialTheme());

  toggleTheme(): void {
    const next: Theme = this.theme() === 'light' ? 'dark' : 'light';
    this.theme.set(next);
    localStorage.setItem(this.storageKey, next);
    this.applyTheme(next);
  }

  applyTheme(theme: Theme): void {
    document.documentElement.setAttribute('data-theme', theme);
  }

  private getInitialTheme(): Theme {
    const stored = localStorage.getItem(this.storageKey) as Theme | null;
    if (stored) return stored;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
