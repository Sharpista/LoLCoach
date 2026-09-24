import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { PlayerContextService } from '../services/player-context.service';
import { ThemeService } from '../services/theme.service';

/** Header sticky com backdrop-blur: brand + nav (Buscar, Dashboard) + toggle de tema. */
@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './header.html',
})
export class Header {
  private readonly context = inject(PlayerContextService);
  readonly theme = inject(ThemeService);

  /** Dashboard do jogador atual, ou a busca se nenhum jogador foi buscado ainda. */
  readonly dashboardLink = computed(() => {
    const id = this.context.currentId;
    return id ? `/player/${id}` : '/';
  });
}
