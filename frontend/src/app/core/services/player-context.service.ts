import { Injectable, signal } from '@angular/core';
import { Player } from '../models/player';

/**
 * Guarda o último jogador buscado, para permitir que o item "Dashboard" do
 * header aponte para o dashboard do jogador atual sem depender de estado
 * global de rotas.
 */
@Injectable({ providedIn: 'root' })
export class PlayerContextService {
  private readonly _current = signal<Player | null>(null);

  readonly current = this._current.asReadonly();

  get currentId(): string | null {
    return this._current()?.id ?? null;
  }

  setPlayer(player: Player): void {
    this._current.set(player);
  }
}
