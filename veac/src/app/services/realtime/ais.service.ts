// Updated ais.service.ts for GoLive endpoints
import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { ConfigService } from '../api/config.service';

export interface AisRequest {
  latitude: number;
  longitude: number;
  radiusKm: number;
  vesselTypes?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AisService {
  private hubConnection!: signalR.HubConnection;
  
  constructor(private configService: ConfigService) {
    this.startConnection();
  }

  private startConnection() {
    const apiUrl = this.configService.getApiBaseUrl();
    const hubUrl = `${apiUrl}/aisHub`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('✅ SignalR Connected to /aisHub');
      })
      .catch(err => {
        console.error('❌ SignalR AIS Connection Error:', err);
      });

    // Listen for all AIS events and log them
    this.hubConnection.on('VesselDataUpdated', (data: any) => {
      console.log('📡 AIS Vessel Data Received:', data);
    });

    this.hubConnection.on('TrackingStarted', (data: any) => {
      console.log('🟢 AIS Tracking Started:', data);
    });

    this.hubConnection.on('TrackingStopped', (data: any) => {
      console.log('🔴 AIS Tracking Stopped:', data);
    });

    this.hubConnection.on('AisProgress', (data: any) => {
      console.log('📊 AIS Progress:', data);
    });

    this.hubConnection.on('AisError', (data: any) => {
      console.error('❌ AIS Error:', data);
    });
  }

  // GoLive API methods
  public async startTracking(request: AisRequest): Promise<any> {
    try {
      const response = await fetch(`${this.configService.getApiBaseUrl()}/api/v1/golive/start-tracking`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`
        },
        body: JSON.stringify(request)
      });

      const result = await response.json();
      if (!result.success) {
        console.error('❌ Start tracking failed:', result.message);
      }
      
      return result;
    } catch (error: any) {
      console.error('❌ Start tracking network error:', error);
      throw error;
    }
  }

  public async stopTracking(): Promise<any> {
    try {
      const response = await fetch(`${this.configService.getApiBaseUrl()}/api/v1/golive/stop-tracking`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`
        }
      });

      const result = await response.json();
      return result;
    } catch (error: any) {
      console.error('❌ Stop tracking network error:', error);
      throw error;
    }
  }

  public async refreshData(request: AisRequest): Promise<any> {
    try {
      const response = await fetch(`${this.configService.getApiBaseUrl()}/api/v1/golive/refresh-data`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`
        },
        body: JSON.stringify(request)
      });

      const result = await response.json();
      return result;
    } catch (error: any) {
      console.error('❌ Refresh data network error:', error);
      throw error;
    }
  }

  public async sendTestData(): Promise<any> {
    try {
      const response = await fetch(`${this.configService.getApiBaseUrl()}/api/v1/golive/send-test-data`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`
        }
      });

      const result = await response.json();

      return result;
    } catch (error: any) {
      console.error('❌ Test data network error:', error);
      throw error;
    }
  }

  public async getStatus(): Promise<any> {
    try {
      const response = await fetch(`${this.configService.getApiBaseUrl()}/api/v1/golive/status`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${this.getAuthToken()}`
        }
      });

      const result = await response.json();
      
      return result;
    } catch (error: any) {
      console.error('❌ Status check network error:', error);
      throw error;
    }
  }

  // Helper method to get auth token (adjust based on your auth implementation)
  private getAuthToken(): string {
    // Replace this with your actual token retrieval logic
    return localStorage.getItem('authToken') || sessionStorage.getItem('authToken') || '';
  }
}