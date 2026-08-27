import { OverlayType, VectorData } from '../../voyage-map-service-type';
import { TooltipData } from './map-tooltip.helper';
import { BaseTooltipHandler } from './base-tooltip.handler';

export class WaveTooltipHandler extends BaseTooltipHandler {
  
  generateTooltip(data: TooltipData): string {
    const height = data.data.height || 0;
    const direction = data.data.direction || 0;
    
    const fromDirection = direction % 360;
    const normalizedFromDirection = ((fromDirection % 360) + 360) % 360;
    const cardinalDir = this.getCardinalDirection(normalizedFromDirection);
    
    const waveScale = this.convertWaveHeightToScale(height);
    const waveDescription = this.getWaveDescription(waveScale);
    const time = this.getFormattedTimeFromData(data);
    const segmentInfo = data.segmentIndex !== undefined ? `Segment ${data.segmentIndex + 1}` : '';

    const waveColor = this.getWaveColorFromHeight(height);
    const intensityText = this.getWaveIntensityText(waveScale);
    const seaState = this.getSeaStateCode(height);
    
    const period = data.data.period || 0;
    const wavelength = period > 0.1 ? this.calculateWaveLength(period) : 0;
    const steepness = wavelength > 0 ? (height / wavelength) * 100 : 0;
    
    const dangerLevel = this.getWaveDangerLevel(height, steepness);
    
    return `
      <div class="tooltip-header">
        <div class="tooltip-icon">🌊</div>
        <div class="tooltip-title">Wave Information</div>
      </div>
      <div class="tooltip-content">
        <div class="tooltip-row">
          <span class="tooltip-label">Height:</span>
          <span class="tooltip-value">
            <span class="tooltip-speed-indicator" style="background-color: ${waveColor}"></span>
            ${height.toFixed(1)} m
          </span>
        </div>
        
        <div class="tooltip-row">
          <span class="tooltip-label">Direction:</span>
          <span class="tooltip-value">From ${cardinalDir} ${Math.round(normalizedFromDirection)}°</span>
        </div>
        
        <div class="tooltip-row">
          <span class="tooltip-label">Sea State:</span>
          <span class="tooltip-value">
            <span class="tooltip-badge" style="background-color: ${waveColor}; color: ${this.getContrastColor(waveColor)};">
              ${seaState}
            </span>
          </span>
        </div>
        
        <div class="tooltip-row">
          <span class="tooltip-label">Intensity:</span>
          <span class="tooltip-value">
            <span class="tooltip-badge" style="background-color: ${waveColor}; color: ${this.getContrastColor(waveColor)};">
              ${intensityText}
            </span>
          </span>
        </div>
        
        <div class="tooltip-row">
          <span class="tooltip-label">Condition:</span>
          <span class="tooltip-value">${waveDescription}</span>
        </div>
        
        ${steepness > 0 && steepness < 100 ? `
        <div class="tooltip-row">
          <span class="tooltip-label">Steepness:</span>
          <span class="tooltip-value">
            ${steepness.toFixed(1)}% 
            <span class="steepness-indicator ${steepness > 7 ? 'danger' : steepness > 4 ? 'warning' : 'safe'}">
              ${steepness > 7 ? '⚠️' : steepness > 4 ? '⚡' : '✅'}
            </span>
          </span>
        </div>
        ` : ''}
        
        <div class="tooltip-row">
          <span class="tooltip-label">Danger Level:</span>
          <span class="tooltip-value">
            <span class="danger-badge ${dangerLevel.class}">
              ${dangerLevel.icon} ${dangerLevel.text}
            </span>
          </span>
        </div>
        
        <div class="tooltip-time"> ${time}</div>
      </div>
    `;
  }

  // Wave-specific methods
  private convertWaveHeightToScale(height: number): number {
    if (height <= 0.1) return 0;
    if (height <= 0.2) return 1;
    if (height <= 0.5) return 2;
    if (height <= 1.25) return 3;
    if (height <= 2.5) return 4;
    if (height <= 4.0) return 5;
    if (height <= 6.0) return 6;
    if (height <= 9.0) return 7;
    if (height <= 14.0) return 8;
    return 9;
  }

  private getWaveDescription(scale: number): string {
    const descriptions = [
      'Mirror-like surface', 'Small ripples', 'Small wavelets', 'Large wavelets',
      'Small waves breaking', 'Moderate waves', 'Large waves forming', 'Sea heaps up', 'Very high waves'
    ];
    return descriptions[scale] || 'Unknown conditions';
  }

  private getWaveIntensityText(scale: number): string {
    const texts = ['CALM', 'SMOOTH', 'SLIGHT', 'MODERATE', 'ROUGH', 'VERY ROUGH', 'HIGH', 'VERY HIGH', 'PHENOMENAL', 'EXCEPTIONAL'];
    return texts[scale] || 'UNKNOWN';
  }

  private getWaveColorFromHeight(height: number): string {
    if (height <= 0.1) return '#E8F5E8';
    if (height <= 0.2) return '#C8E6C9';
    if (height <= 0.5) return '#81C784';
    if (height <= 1.0) return '#4CAF50';
    if (height <= 1.25) return '#2196F3';
    if (height <= 2.0) return '#03A9F4';
    if (height <= 2.5) return '#FF9800';
    if (height <= 4.0) return '#FF5722';
    if (height <= 6.0) return '#F44336';
    if (height <= 9.0) return '#E91E63';
    return '#9C27B0';
  }

  private getSeaStateCode(height: number): string {
    if (height <= 0.1) return 'Code 0 - Calm';
    if (height <= 0.2) return 'Code 1 - Smooth';
    if (height <= 0.5) return 'Code 2 - Slight';
    if (height <= 1.25) return 'Code 3 - Moderate';
    if (height <= 2.5) return 'Code 4 - Rough';
    if (height <= 4.0) return 'Code 5 - Very Rough';
    if (height <= 6.0) return 'Code 6 - High';
    if (height <= 9.0) return 'Code 7 - Very High';
    if (height <= 14.0) return 'Code 8 - Phenomenal';
    return 'Code 9 - Exceptional';
  }

  private calculateWaveLength(period: number): number {
    if (period <= 0.001) return 0;
    const g = 9.81;
    return (g * period * period) / (2 * Math.PI);
  }

  private getWaveDangerLevel(height: number, steepness: number): { class: string; icon: string; text: string } {
    const criticalSteepness = steepness > 7;
    
    if (height >= 6.0 || criticalSteepness) {
      return { class: 'danger-critical', icon: '🚨', text: 'CRITICAL' };
    }
    if (height >= 4.0 || steepness > 5) {
      return { class: 'danger-high', icon: '⚠️', text: 'HIGH' };
    }
    if (height >= 2.0 || steepness > 3) {
      return { class: 'danger-moderate', icon: '⚡', text: 'MODERATE' };
    }
    if (height >= 1.0) {
      return { class: 'danger-low', icon: '⚪', text: 'LOW' };
    }
    return { class: 'danger-minimal', icon: '✅', text: 'MINIMAL' };
  }
}
