import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { apiBaseUrl } from '../config/api-config';
import { Player, SearchPlayerRequest } from '../models/player';

/** Serviço de busca de jogador (spec 001) — POST /api/players/search. */
@Injectable({ providedIn: 'root' })
export class PlayerSearchService {
  private readonly http = inject(HttpClient);

  search(request: SearchPlayerRequest): Observable<Player> {
    return this.http.post<Player>(`${apiBaseUrl()}/api/players/search`, request);
  }
}
