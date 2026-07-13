import { ApplicationConfig, provideZoneChangeDetection, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { ConfigService } from './core/config.service';

import { routes } from './app.routes';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';

// Factory function to load config before app starts
export function initializeApp(configService: ConfigService) {
  return () => {
    return fetch('/config.json')
      .then(response => response.json())
      .then(config => {
        configService.setConfig(config);
      })
      .catch(() => { console.warn('Failed to load config.json, using defaults'); });
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }), 
    provideRouter(routes), 
    provideAnimationsAsync(),
    provideHttpClient(withInterceptorsFromDi()),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [ConfigService],
      multi: true
    },
    // Hide the Material required asterisk app-wide (validation is kept; fields are auto-populated).
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { hideRequiredMarker: true } }
  ]
};
