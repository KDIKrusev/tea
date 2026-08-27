import { Injectable } from '@angular/core';
import { TimeSyncService } from './time-sync.service';

@Injectable({
  providedIn: 'root'
})
export class UtcTimeService {

  constructor(
    private timeSyncService: TimeSyncService
  ) { }

  public GetUtcTimestamp(date: Date): number {
    const ts = Date.UTC(date.getFullYear(), date.getMonth(), date.getDate(), date.getHours(), date.getMinutes(), date.getSeconds(), date.getMilliseconds());
    // backend timestamps are in microseconds and JS timestamps are in milliseconds
    return ts * 1000;
  }

  public GetUtcDateToday(): Date {
    const now = new Date(this.timeSyncService.systemTime);
    const timestamp = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate());
    return new Date(timestamp);
  }

  public GetUtcDate(timestamp: number): Date {
    const ts = Math.floor(timestamp / 1000);
    const result = new Date(ts + new Date(ts).getTimezoneOffset() * 60000);
    return result;
  }

  public GetCurrentUtcDate(): Date {
    const now = this.timeSyncService.systemTimeMicroS;
    const result = this.GetUtcDate(now);
    return result;
  }

  public ConvertDateToUtc(date: Date): Date {
    const timestamp = Date.UTC(date.getFullYear(), date.getMonth(), date.getDate());
    return new Date(timestamp);
  }

  public ConvertDateTimeToUtc(date: Date): Date {
    const ts = date.getTime();
    const timestamp = new Date(ts + new Date(ts).getTimezoneOffset() * 60000);
    return new Date(timestamp);
  }
}
