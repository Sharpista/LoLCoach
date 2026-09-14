import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { Player } from '../../core/models/player';
import { PlayerContextService } from '../../core/services/player-context.service';
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

  it('deve contar caracteres suplementares por code points nos limites do gameName', () => {
    const { component } = createComponent();
    const supplementaryLetter = '𐐀';
    const validInput = (gameName: string) =>
      component.form.setValue({ gameName, tagLine: 'BR', region: 'br1' });

    validInput(supplementaryLetter.repeat(3));
    expect(component.form.controls.gameName.valid).toBe(true);

    validInput(supplementaryLetter.repeat(16));
    expect(component.form.controls.gameName.valid).toBe(true);

    validInput(supplementaryLetter.repeat(2));
    expect(component.form.controls.gameName.hasError('gameNameLength')).toBe(true);
    expect(component.form.controls.gameName.getError('gameNameLength').actual).toBe(2);

    validInput(supplementaryLetter.repeat(17));
    expect(component.form.controls.gameName.hasError('gameNameLength')).toBe(true);
    expect(component.form.controls.gameName.getError('gameNameLength').actual).toBe(17);
  });

  it('deve rejeitar plataforma fora do conjunto suportado', () => {
    const { component } = createComponent();
    component.form.setValue({ gameName: 'Faker', tagLine: 'KR1', region: 'invalid' });

    expect(component.form.invalid).toBe(true);
    expect(component.form.controls.region.hasError('regionUnsupported')).toBe(true);
  });

  it('deve chamar o serviço e navegar para /player/:id em sucesso', () => {
    searchSpy.mockReturnValue(of(player));
    const { component } = createComponent();
    component.form.setValue({ gameName: '  Faker  ', tagLine: 'KR1', region: 'kr' });
    component.onSubmit();
    expect(searchSpy).toHaveBeenCalledWith({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });
    expect(TestBed.inject(PlayerContextService).current()).toEqual(player);
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

  it.each([
    [429, 'Muitas requisições'],
    [503, 'serviço da Riot está indisponível'],
    [500, 'Erro inesperado'],
  ])('deve mapear HTTP %s para mensagem amigável', (status, message) => {
    searchSpy.mockReturnValue(throwError(() => new HttpErrorResponse({ status })));
    const { component } = createComponent();
    component.form.setValue({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });

    component.onSubmit();

    expect(component.errorMessage()).toContain(message);
    expect(component.loading()).toBe(false);
  });

  it('deve normalizar espaços e caixa da plataforma no payload', () => {
    searchSpy.mockReturnValue(of(player));
    const { component } = createComponent();
    component.form.setValue({ gameName: '  Faker  ', tagLine: ' KR1 ', region: ' KR ' });

    component.onSubmit();

    expect(searchSpy).toHaveBeenCalledWith({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });
  });

  it('deve exibir erros de campos retornados em ProblemDetails', () => {
    searchSpy.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { errors: { gameName: ['Nome inválido'], region: ['Plataforma inválida'] } },
          }),
      ),
    );
    const { component } = createComponent();
    component.form.setValue({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });

    component.onSubmit();

    expect(component.fieldErrors()).toEqual({
      gameName: ['Nome inválido'],
      region: ['Plataforma inválida'],
    });
    expect(component.loading()).toBe(false);
  });

  it('não deve iniciar uma segunda busca enquanto está carregando', () => {
    const subject = new Subject<Player>();
    searchSpy.mockReturnValue(subject.asObservable());
    const { component } = createComponent();
    component.form.setValue({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });

    component.onSubmit();
    component.onSubmit();

    expect(searchSpy).toHaveBeenCalledTimes(1);
    subject.complete();
  });
});
