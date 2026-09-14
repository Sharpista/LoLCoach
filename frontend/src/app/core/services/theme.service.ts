import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'lolcoach-theme';
const LIGHT_COLOR = '#faf8f5';
const DARK_COLOR = '#0c0a09';

/** Alterna o tema claro/escuro adicionando a classe `dark` ao `<html>`. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly isDark = signal(this.readInitial());

  constructor() {
    this.apply(this.isDark());
  }

  toggle(): void {
    this.apply(!this.isDark());
  }

  private apply(dark: boolean): void {
    this.isDark.set(dark);
    document.documentElement.classList.toggle('dark', dark);

    const meta = document.querySelector('meta[name="theme-color"]');
    meta?.setAttribute('content', dark ? DARK_COLOR : LIGHT_COLOR);

    try {
      // Grava apenas quando o valor difere do já persistido (evita reescrita
      // redundante no bootstrap, quando o script inline já aplicou o tema).
      const value = dark ? 'dark' : 'light';
      if (localStorage.getItem(STORAGE_KEY) !== value) {
        localStorage.setItem(STORAGE_KEY, value);
      }
    } catch {
      // Armazenamento indisponível — o tema segue funcionando para a sessão.
    }
  }

  /** Espelha o estado já aplicado pelo script inline no index.html. */
  private readInitial(): boolean {
    if (typeof document === 'undefined') {
      return false;
    }
    return document.documentElement.classList.contains('dark');
  }
}
