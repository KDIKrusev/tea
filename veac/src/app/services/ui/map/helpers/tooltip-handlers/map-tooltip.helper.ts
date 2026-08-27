import { Injectable } from '@angular/core';
import { Coordinate } from 'ol/coordinate';
import { Pixel } from 'ol/pixel';
import { OverlayType, VectorData } from '../../voyage-map-service-type';
import { WaveTooltipHandler } from './wave-tooltip.handler';
import { WindTooltipHandler } from './wind-tooltip.handler';
import { CurrentTooltipHandler } from './current-tooltip.handler';
import { VesselTooltipHandler } from './vessel-tooltip.handler';
import {FavorableWeatherIndexTooltipHandler} from './weather-tooltip.handler';
import {RouteSegment} from '../../../../../models/entities/route-segment.model';

export interface TooltipData {
  overlayType: OverlayType;
  data: VectorData;
  coordinate: Coordinate;
  pixel: Pixel;
  startTime?: Date;      
  endTime?: Date;     
  segmentIndex?: number;
  routeSegments?: RouteSegment[]; 
}

@Injectable({
  providedIn: 'root'
})
export class MapTooltipHelper {
  private tooltipElement: HTMLElement | null = null;
  private currentTooltip: TooltipData | null = null;
  private isTooltipVisible = false;

  // Tooltip handlers
  private readonly waveHandler = new WaveTooltipHandler();
  private readonly windHandler = new WindTooltipHandler();
  private readonly currentHandler = new CurrentTooltipHandler();
  private readonly vesselHandler = new VesselTooltipHandler();

  constructor(private weatherHandler: FavorableWeatherIndexTooltipHandler) {}

  showTooltip(data: TooltipData, mapElement: HTMLElement): void {
    this.currentTooltip = data;
    
    if (!this.tooltipElement) {
      this.createTooltipElement(mapElement);
    }
    
    this.updateTooltipContent(data);
    this.positionTooltip(data.pixel, mapElement);
    this.showTooltipElement();
  }

  hideTooltip(): void {
    if (this.tooltipElement) {
      this.tooltipElement.style.display = 'none';
      this.isTooltipVisible = false;
    }
    this.currentTooltip = null;
  }

  updateTooltipPosition(pixel: Pixel, mapElement: HTMLElement): void {
    if (this.isTooltipVisible && this.tooltipElement) {
      this.positionTooltip(pixel, mapElement);
    }
  }

  private updateTooltipContent(data: TooltipData): void {
    if (!this.tooltipElement) return;
    
    const content = this.generateTooltipContent(data);
    this.tooltipElement.innerHTML = content;
    
  }

   private generateTooltipContent(data: TooltipData): string {
    switch (data.overlayType) {
      case 'vessel':
        return this.vesselHandler.generateTooltip(data);
      case 'wind':
        return this.windHandler.generateTooltip(data);
      case 'current':
        return this.currentHandler.generateTooltip(data);
      case 'waves':
        return this.waveHandler.generateTooltip(data);
      case 'weather':
        return this.weatherHandler.generateTooltip(data);
      default:
        return '<div>Unknown overlay type</div>';
    }
  }

  private createTooltipElement(mapElement: HTMLElement): void {
    this.tooltipElement = document.createElement('div');
    this.tooltipElement.className = 'voyage-map-tooltip';
    this.tooltipElement.style.cssText = `
      position: absolute;
      background: rgba(0, 0, 0, 0.95);
      color: white;
      padding: 0;
      border-radius: 12px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      font-size: 13px;
      line-height: 1.4;
      max-width: 320px;
      z-index: 1000;
      pointer-events: none;
      display: none;
      backdrop-filter: blur(8px);
      border: 1px solid rgba(255, 255, 255, 0.1);
      overflow: hidden;
      transform: translateY(-5px);
      transition: all 0.2s ease-out;
    `;
    
    mapElement.appendChild(this.tooltipElement);
    this.addTooltipStyles();
  }

  private addTooltipStyles(): void {
    if (document.head.querySelector('#voyage-tooltip-styles')) return;
    
    const style = document.createElement('style');
    style.id = 'voyage-tooltip-styles';
    style.textContent = `
      .voyage-map-tooltip {
        animation: tooltip-fade-in 0.2s ease-out;
      }
      
      @keyframes tooltip-fade-in {
        from { opacity: 0; transform: translateY(-10px); }
        to { opacity: 1; transform: translateY(-5px); }
      }
      
      .tooltip-header {
        display: flex;
        align-items: center;
        gap: 10px;
        padding: 14px 16px 12px;
        background: linear-gradient(135deg, rgba(255, 255, 255, 0.1), rgba(255, 255, 255, 0.05));
        border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        margin: 0;
      }
      
      .tooltip-icon {
        font-size: 18px;
        display: flex;
        align-items: center;
        justify-content: center;
        width: 24px;
        height: 24px;
        border-radius: 50%;
        background: rgba(255, 255, 255, 0.1);
      }
      
      .tooltip-title {
        font-weight: 600;
        font-size: 14px;
        color: white;
        margin: 0;
      }
      
      .tooltip-content {
        padding: 12px 16px 14px;
        display: flex;
        flex-direction: column;
        gap: 8px;
      }
      
      .tooltip-row {
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 16px;
        padding: 2px 0;
      }
      
      .tooltip-label {
        color: rgba(255, 255, 255, 0.7);
        font-size: 12px;
        min-width: 85px;
        font-weight: 500;
      }
      
      .tooltip-value {
        font-weight: 600;
        text-align: right;
        color: white;
        font-size: 13px;
      }
      
      .tooltip-badge {
        display: inline-flex;
        align-items: center;
        gap: 6px;
        padding: 4px 8px;
        border-radius: 12px;
        font-size: 11px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.5px;
      }
      
      .tooltip-speed-indicator {
        display: inline-block;
        width: 8px;
        height: 8px;
        border-radius: 50%;
        margin-right: 6px;
      }
      
      .steepness-indicator {
        margin-left: 4px;
        font-size: 10px;
      }
      
      .danger-badge {
        display: inline-flex;
        align-items: center;
        gap: 4px;
        padding: 2px 6px;
        border-radius: 8px;
        font-size: 10px;
        font-weight: 700;
        text-transform: uppercase;
      }
      
      .danger-minimal { background: #4CAF50; color: white; }
      .danger-low { background: #FF9800; color: white; }
      .danger-moderate { background: #FF5722; color: white; }
      .danger-high { background: #F44336; color: white; }
      .danger-critical { background: #D32F2F; color: white; animation: pulse 1s infinite; }
      
      @keyframes pulse {
        0%, 100% { opacity: 1; }
        50% { opacity: 0.7; }
      }
      
      .tooltip-time {
        font-size: 11px;
        color: rgba(255, 255, 255, 0.6);
        text-align: center;
        padding-top: 8px;
        border-top: 1px solid rgba(255, 255, 255, 0.1);
        margin-top: 8px;
      }
    `;
    
    document.head.appendChild(style);
  }

  private positionTooltip(pixel: Pixel, mapElement: HTMLElement): void {
    if (!this.tooltipElement) return;
    
    const mapRect = mapElement.getBoundingClientRect();
    const tooltipRect = this.tooltipElement.getBoundingClientRect();
    
    let left = pixel[0] + 15;
    let top = pixel[1] - tooltipRect.height - 10;
    
    if (left + tooltipRect.width > mapRect.width) {
      left = pixel[0] - tooltipRect.width - 15;
    }
    
    if (top < 0) {
      top = pixel[1] + 15;
    }
    
    this.tooltipElement.style.left = `${left}px`;
    this.tooltipElement.style.top = `${top}px`;
  }

  private showTooltipElement(): void {
    if (this.tooltipElement) {
      this.tooltipElement.style.display = 'block';
      this.isTooltipVisible = true;
    }
  }

  destroy(): void {
    if (this.tooltipElement) {
      this.tooltipElement.remove();
      this.tooltipElement = null;
    }
    this.currentTooltip = null;
    this.isTooltipVisible = false;
  }
}