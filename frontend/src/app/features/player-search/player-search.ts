import { KeyValuePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { PlayerContextService } from '../../core/services/player-context.service';
import { PlayerSearchService } from '../../core/services/player-search.service';
import { ProblemDetails } from '../../core/models/player';
import { REGIONS } from './regions';
import { gameNameValidator, tagLineValidator } from './riot-id.validators';

type FieldName = 'gameName' | 'tagLine' | 'region';

/** Mensagem amigável por status HTTP (Problem Details nunca vaza stack/chave). */
function mapSearchError(err: HttpErrorResponse): string {
  switch (err.status) {
    case 400:
      return 'Dados inválidos. Verifique os campos e tente novamente.';
    case 404:
      return 'Jogador não encontrado. Confira o Riot ID e a região informada.';
    case 429:
      return 'Muitas requisições no momento. Aguarde um instante e tente novamente.';
    case 503:
      return 'O serviço da Riot está indisponível agora. Tente novamente em breve.';
    default:
      return 'Erro inesperado ao buscar o jogador. Tente novamente.';
  }
}

/** Tela de busca (spec 001): Riot ID + região -> POST /api/players/search. */
@Component({
  selector: 'app-player-search',
  imports: [ReactiveFormsModule, KeyValuePipe],
  templateUrl: './player-search.html',
  styleUrl: './player-search.scss',
})
export class PlayerSearch {
  private readonly fb = inject(FormBuilder);
  private readonly searchService = inject(PlayerSearchService);
  private readonly context = inject(PlayerContextService);
  private readonly router = inject(Router);

  readonly regions = REGIONS;

  readonly form = this.fb.nonNullable.group({
    gameName: ['', [Validators.required, gameNameValidator()]],
    tagLine: ['', [Validators.required, tagLineValidator()]],
    region: ['', [Validators.required]],
  });

  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  /** Erros de validação vindos do backend (Problem Details 400, chaves camelCase). */
  readonly fieldErrors = signal<Record<string, string[]>>({});
  readonly hasFieldErrors = computed(() => Object.keys(this.fieldErrors()).length > 0);
  readonly submitted = signal(false);

  errorFor(field: FieldName): string | null {
    const control = this.form.get(field);
    if (!control?.errors || !(control.touched || this.submitted())) {
      return null;
    }
    const e = control.errors;
    if (e['required']) {
      return 'Campo obrigatório.';
    }
    if (e['gameNameLength']) {
      return 'Entre 3 e 16 caracteres.';
    }
    if (e['tagLineLength']) {
      return 'Entre 2 e 5 caracteres.';
    }
    if (e['gameNamePattern']) {
      return 'Apenas letras, números e espaços.';
    }
    if (e['tagLinePattern']) {
      return 'Apenas letras e números.';
    }
    return null;
  }

  onSubmit(): void {
    this.submitted.set(true);
    this.errorMessage.set(null);
    this.fieldErrors.set({});

    if (this.form.invalid) {
      return;
    }

    this.loading.set(true);
    const raw = this.form.getRawValue();
    this.searchService
      .search({
        gameName: raw.gameName.trim(),
        tagLine: raw.tagLine.trim(),
        region: raw.region,
      })
      .subscribe({
        next: (player) => {
          this.loading.set(false);
          this.context.setPlayer(player);
          void this.router.navigate(['/player', player.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          this.errorMessage.set(mapSearchError(err));
          const problem: ProblemDetails | undefined = err.error;
          if (problem?.errors) {
            this.fieldErrors.set(problem.errors);
          }
        },
      });
  }
}
