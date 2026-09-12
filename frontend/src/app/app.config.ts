import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { MockPlayerDashboardService } from './core/services/mock-player-dashboard.service';
import { PlayerDashboardService } from './core/services/player-dashboard.service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    // Spec 004: backend 002/003 ainda não existe. Troque o useClass pelo
    // HttpPlayerDashboardService quando a API real for implementada.
    { provide: PlayerDashboardService, useClass: MockPlayerDashboardService },
  ],
};
