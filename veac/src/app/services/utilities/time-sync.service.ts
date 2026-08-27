import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class TimeSyncService {
  public systemTime!: number;
  public systemTimeMicroS!: number;

  constructor(
    ) {
      this.updateSystemTime();
  }


  public newSystemDate(): Date {
    return new Date(this.systemTime);
  }

  private updateSystemTime(): void {
    const now = new Date();
    this.systemTime = now.getTime(); // Current time in milliseconds since Unix epoch (UTC)
    this.systemTimeMicroS = this.systemTime * 1000; // Convert to microseconds
  }

  public refreshSystemTime(): void {
    this.updateSystemTime();
  }

}
