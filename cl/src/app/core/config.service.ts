import { Injectable } from '@angular/core';

export interface AppConfig {
  apiUrl: string;
}

@Injectable({
  providedIn: 'root'
})
export class ConfigService {
  private config: AppConfig = {
    apiUrl: 'https://localhost:7197' // Default fallback
  };

  setConfig(config: AppConfig): void {
    this.config = config;
  }

  get apiUrl(): string {
    return this.config.apiUrl;
  }
}
