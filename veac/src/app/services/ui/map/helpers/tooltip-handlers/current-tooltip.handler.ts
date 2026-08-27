import { TooltipData } from './map-tooltip.helper';
import { BaseTooltipHandler } from './base-tooltip.handler';

export class CurrentTooltipHandler extends BaseTooltipHandler {
  
  generateTooltip(data: TooltipData): string {
    const speed = data.data.speed || 0;
    const direction = data.data.direction || 0;
    
    const fromDirection = direction % 360;
    const normalizedFromDirection = ((fromDirection % 360) + 360) % 360;
    const cardinalDir = this.getCardinalDirection(normalizedFromDirection);
    
    const currentScale = this.convertCurrentSpeedToScale(speed);
    const currentDescription = this.getCurrentDescription(currentScale);
    const speedKnots = (speed * 1.944).toFixed(1);
    const time = this.getFormattedTimeFromData(data);
    const segmentInfo = data.segmentIndex !== undefined ? `Segment ${data.segmentIndex + 1}` : '';
    const currentColor = this.getCurrentColorFromSpeed(speed);
    const intensityText = this.getCurrentIntensityText(currentScale);
    
    return `
      <div class="tooltip-header">
        <div class="tooltip-icon">🌊</div>
        <div class="tooltip-title">Ocean Current</div>
      </div>
      <div class="tooltip-content">
        <div class="tooltip-row">
          <span class="tooltip-label">Speed:</span>
          <span class="tooltip-value">
            <span class="tooltip-speed-indicator" style="background-color: ${currentColor}"></span>
            ${speed.toFixed(1)} m/s (${speedKnots} kts)
          </span>
        </div>
        <div class="tooltip-row">
          <span class="tooltip-label">Direction:</span>
          <span class="tooltip-value">From ${cardinalDir} ${Math.round(normalizedFromDirection)}°</span>
        </div>
        <div class="tooltip-row">
          <span class="tooltip-label">Strength:</span>
          <span class="tooltip-value">
            <span class="tooltip-badge" style="background-color: ${currentColor}; color: ${this.getContrastColor(currentColor)};">
              ${intensityText}
            </span>
          </span>
        </div>
        <div class="tooltip-row">
          <span class="tooltip-label">Condition:</span>
          <span class="tooltip-value">${currentDescription}</span>
        </div>
        <div class="tooltip-time">${time}</div>
      </div>
    `;
  }

  // Current-specific methods
  private convertCurrentSpeedToScale(currentSpeedMs: number): number {
    if (currentSpeedMs <= 0.2) return 0;
    if (currentSpeedMs <= 0.5) return 1;
    if (currentSpeedMs <= 1.0) return 2;
    if (currentSpeedMs <= 1.5) return 3;
    if (currentSpeedMs <= 2.0) return 4;
    return 5;
  }

  private getCurrentDescription(scale: number): string {
    const descriptions = [
      'Negligible current', 'Weak current', 'Moderate current',
      'Strong current', 'Very strong current', 'Extreme current'
    ];
    return descriptions[scale] || 'Unknown current';
  }

  private getCurrentIntensityText(scale: number): string {
    const texts = ['NEGLIGIBLE', 'WEAK', 'MODERATE', 'STRONG', 'VERY STRONG', 'EXTREME'];
    return texts[scale] || 'UNKNOWN';
  }

  private getCurrentColorFromSpeed(speed: number): string {
    if (speed <= 0.2) return '#F3E5F5';
    if (speed <= 0.5) return '#CE93D8';
    if (speed <= 1.0) return '#AB47BC';
    if (speed <= 1.5) return '#8E24AA';
    if (speed <= 2.0) return '#6A1B9A';
    return '#4A148C';
  }
}