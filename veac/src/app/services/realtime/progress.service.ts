import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';
import { ConfigService } from '../api/config.service';

@Injectable({
  providedIn: 'root'
})
export class ProgressService {
  private hubConnection!: signalR.HubConnection;
  public progress$ = new BehaviorSubject<number>(0);
  public description$ = new BehaviorSubject<string>(''); 

  constructor(private configService: ConfigService) {
    this.startConnection();
  }

  private startConnection() {
    const apiUrl = this.configService.getApiBaseUrl();
    const hubUrl = `${apiUrl}/progressHub`;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl) 
      .withAutomaticReconnect()
      .build();

    this.hubConnection
      .start()
      .then(() => console.log('✅ SignalR Connected to /progressHub'))
      .catch(err => console.error('❌ SignalR Connection Error:', err));

    // Listen for progress updates
     this.hubConnection.on('UpdateProgress', (data: { progress: number, description: string }) => {
      this.progress$.next(data.progress);
      this.description$.next(data.description);
    });
  }

  resetProgress() {
    this.progress$.next(0);
    this.description$.next('');
    this.description$.next('');
  }
}
