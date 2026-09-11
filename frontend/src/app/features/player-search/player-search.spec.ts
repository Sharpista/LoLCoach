import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Player } from '../../core/models/player';
import { PlayerSearchService } from '../../core/services/player-search.service';
import { PlayerSearch } from './player-search';

const player: Player = {
  id: '11111111-1111-1111-1111-111111111111',
  puuid: 'puuid-x',
  gameName: 'Faker',
  tagLine: 'KR1',
  region: 'kr',
  createdAt: '2026-01-01T00:00:00Z',
  lastUpdatedAt: '2026-01-01T00:00:00Z',
};

describe('PlayerSearch', () => {
  let searchSpy: ReturnType<typeof vi.fn>;
  let router: Router;

  beforeEach(async () => {
    searchSpy = vi.fn();

    await TestBed.configureTestingModule({
      imports: [PlayerSearch],
      providers: [
        provideRouter([]),
        { provide: PlayerSearchService, useValue: { search: searchSpy } },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  function createComponent() {
    const fixture = TestBed.createComponent(PlayerSearch);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance };
  }

  it('deve invalidar o formulário vazio e não chamar o serviço', () => {
    const { component } = createComponent();
    component.onSubmit();
    expect(component.form.invalid).toBe(true);
    expect(searchSpy).not.toHaveBeenCalled();
  });

  it('deve aceitar gameName Unicode/espaços (3-16) e tagLine legado (2-5)', () => {
    const { component } = createComponent();
    component.form.setValue({ gameName: 'Fä ker', tagLine: 'BR', region: 'br1' });
    expect(component.form.valid).toBe(true);

    component.form.setValue({ gameName: 'ab', tagLine: 'A', region: 'br1' });
    expect(component.form.invalid).toBe(true);
  });

  it('deve chamar o serviço e navegar para /player/:id em sucesso', () => {
    searchSpy.mockReturnValue(of(player));
    const { component } = createComponent();
    component.form.setValue({ gameName: '  Faker  ', tagLine: 'KR1', region: 'kr' });
    component.onSubmit();
    expect(searchSpy).toHaveBeenCalledWith({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });
    expect(router.navigate).toHaveBeenCalledWith(['/player', player.id]);
  });

  it('deve exibir loading enquanto a busca está em andamento', () => {
    const subject = new Subject<Player>();
    searchSpy.mockReturnValue(subject.asObservable());
    const { component } = createComponent();
    component.form.setValue({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });
    component.onSubmit();
    expect(component.loading()).toBe(true);
    subject.next(player);
    subject.complete();
    expect(component.loading()).toBe(false);
  });

  it('deve mapear 404 para mensagem amigável', () => {
    searchSpy.mockReturnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    const { component } = createComponent();
    component.form.setValue({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });
    component.onSubmit();
    expect(component.errorMessage()).toContain('Jogador não encontrado');
  });
});
