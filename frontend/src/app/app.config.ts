import { provideHttpClient } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { HttpPlayerDashboardService } from './core/services/http-player-dashboard.service';
import { PlayerDashboardService } from './core/services/player-dashboard.service';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(),
    { provide: PlayerDashboardService, useClass: HttpPlayerDashboardService },
  ],
};
