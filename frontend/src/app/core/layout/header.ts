import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { PlayerContextService } from '../services/player-context.service';

/** Header sticky com backdrop-blur: brand + nav (Buscar, Dashboard). */
@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './header.html',
  styleUrl: './header.scss',
})
export class Header {
  private readonly context = inject(PlayerContextService);

  /** Dashboard do jogador atual, ou a busca se nenhum jogador foi buscado ainda. */
  readonly dashboardLink = computed(() => {
    const id = this.context.currentId;
    return id ? `/player/${id}` : '/';
  });
}
