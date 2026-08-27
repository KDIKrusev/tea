// live-vessel-details.component.ts
import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CurrentPosition } from '../../models/entities/current-vessel-position.model';

export interface LiveTrackingInfo {
  eta: number | null;
  remainingTimeInSeconds: number | null;
  currentSpeed: number | null;
  progress: number;
}

export enum DataStatus {
  LIVE = 'live',
  STALE = 'stale',
  UNAVAILABLE = 'unavailable'
}

@Component({
  selector: 'app-live-vessel-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: "live-vessel-details.component.html",
  styleUrls: ['./live-vessel-details.component.css']
})
export class LiveVesselDetailsComponent {
  @Input() currentVesselPosition: CurrentPosition | null = null;
  @Input() liveTrackingInfo: LiveTrackingInfo = {
    eta: null,
    remainingTimeInSeconds: null,
    currentSpeed: null,
    progress: 0
  };
  
  private readonly MOVEMENT_THRESHOLD_SPEED = 0.3; // knots

  hasValidPosition(): boolean {
    return !!(
      this.currentVesselPosition?.latitude && 
      this.currentVesselPosition?.longitude &&
      this.currentVesselPosition.latitude !== 0 &&
      this.currentVesselPosition.longitude !== 0
    );
  }

  isVesselMoving(): boolean {
    return (this.currentVesselPosition?.status === "Under way using engine");
  }

  getDataStatus(): DataStatus {
    if (!this.hasValidPosition()) {
      return DataStatus.UNAVAILABLE;
    }

    if (!this.currentVesselPosition?.positionUpdatedAt) {
      return DataStatus.UNAVAILABLE;
    }
    return DataStatus.LIVE;
  }

  getDataStatusClass(): string {
    const status = this.getDataStatus();
    return `status-${status}`;
  }

  getDataStatusText(): string {
    const status = this.getDataStatus();
    switch (status) {
      case DataStatus.LIVE:
        return 'AIS Data Available';
      case DataStatus.STALE:
        return 'Data Outdated';
      case DataStatus.UNAVAILABLE:
        return 'Waiting for AIS Data';
      default:
        return 'Unknown Status';
    }
  }

  getVesselStatusClass(): string {
    if (this.isVesselMoving()) {
      return 'status-moving';
    }
    return 'status-stationary';
  }

  getVesselStatusText(): string {
    if (this.isVesselMoving()) {
      return 'Moving';
    }
    return 'Stationary';
  }

  getMinutesSinceLastUpdate(): number {
    if (!this.currentVesselPosition?.positionUpdatedAt) {
      return 0;
    }

    const now = new Date();
    const lastUpdate = new Date(this.currentVesselPosition.positionUpdatedAt);
    return Math.floor((now.getTime() - lastUpdate.getTime()) / (1000 * 60));
  }

  getTimeSinceLastUpdate(): string {
    const minutes = this.getMinutesSinceLastUpdate();
    
    if (minutes < 1) {
      return 'Just now';
    } else if (minutes < 60) {
      return `${minutes} minute${minutes !== 1 ? 's' : ''} ago`;
    } else {
      const hours = Math.floor(minutes / 60);
      const remainingMinutes = minutes % 60;
      if (remainingMinutes === 0) {
        return `${hours} hour${hours !== 1 ? 's' : ''} ago`;
      } else {
        return `${hours}h ${remainingMinutes}m ago`;
      }
    }
  }

  formatRemainingTime(seconds: number): string {
    if (!seconds || seconds <= 0) return 'N/A';
    
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    
    if (hours > 0) {
      return `${hours}h ${minutes}m`;
    } else if (minutes > 0) {
      return `${minutes}m ${remainingSeconds}s`;
    } else {
      return `${remainingSeconds}s`;
    }
  }
}