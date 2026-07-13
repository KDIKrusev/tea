import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

// Bootstrap application - config will be loaded via APP_INITIALIZER
bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
