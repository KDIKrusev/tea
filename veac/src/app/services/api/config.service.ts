import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private apiUrl: string = '';

  constructor() {
    this.loadConfig();
  }

  private loadConfig() {
    this.apiUrl = (window as any)['API_URL'] || '';
    console.log(`✅ API Base URL Set: ${this.apiUrl}`);
  }

  getApiBaseUrl(): string {
    return this.apiUrl;
  }
}