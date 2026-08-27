import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

async function loadCompassComponent() {
  await import('@ocean-industries-concept-lab/openbridge-webcomponents/dist/navigation-instruments/compass/compass.js');
}

async function fetchConfig() {
  try {
    const response = await fetch('/assets/config.json');
    const config = await response.json();

    (window as any)['API_URL'] = (window as any)['API_URL'] || config.apiUrl;

  } catch (error) {
    console.error('❌ Failed to load configuration:', error);
  }
}

Promise.all([
  fetchConfig(),
  loadCompassComponent(), // ✅ Dynamically import the component
]).then(() => {
  bootstrapApplication(AppComponent, appConfig)
    .catch(err => console.error(err));
});

// fetchConfig().then(() => {
//   bootstrapApplication(AppComponent, appConfig)
//     .catch((err) => console.error(err));
// });
