import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { PlayerSearchService } from './player-search.service';

describe('PlayerSearchService', () => {
  let service: PlayerSearchService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PlayerSearchService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PlayerSearchService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('deve fazer POST em /api/players/search com o corpo correto', () => {
    let result: unknown;
    service
      .search({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' })
      .subscribe((r) => (result = r));

    const req = httpMock.expectOne('http://localhost:5181/api/players/search');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ gameName: 'Faker', tagLine: 'KR1', region: 'kr' });

    req.flush({ id: '11111111-1111-1111-1111-111111111111', gameName: 'Faker' });
    expect(result).toBeTruthy();
  });
});
