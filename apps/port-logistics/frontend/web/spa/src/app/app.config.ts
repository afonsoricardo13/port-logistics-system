import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection, importProvidersFrom, APP_INITIALIZER } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { routes } from './app.routes';
import { authInterceptor } from './interceptors/auth.interceptor';
import { translations } from './config/translations';

// Initialize translations globally
function initializeTranslations(translate: TranslateService) {
  return () => {
    translate.setTranslation('en', translations.en);
    translate.setTranslation('pt', translations.pt);
    translate.setDefaultLang('en');
    
    const savedLang = localStorage.getItem('language') || 'en';
    translate.use(savedLang);
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor])
    ),
    importProvidersFrom(
      TranslateModule.forRoot({
        defaultLanguage: 'en'
      })
    ),
    {
      provide: APP_INITIALIZER,
      useFactory: initializeTranslations,
      deps: [TranslateService],
      multi: true
    }
  ]
};
