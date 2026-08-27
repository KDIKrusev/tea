import {BaseTooltipHandler} from '../tooltip-handlers/base-tooltip.handler'
import {TooltipData} from '../tooltip-handlers/map-tooltip.helper'

export class WindTooltipHandler extends BaseTooltipHandler {
  
  generateTooltip(data: TooltipData): string {
    const speed = data.data.speed || 0;
    const direction = data.data.direction || 0;
    
    const fromDirection = direction % 360;
    const normalizedFromDirection = ((fromDirection % 360) + 360) % 360;
    const cardinalDir = this.getCardinalDirection(normalizedFromDirection);
    
    const beaufortScale = this.convertWindSpeedToBeaufort(speed);
    const beaufortDescription = this.getBeaufortDescription(beaufortScale);
    const speedKnots = (speed * 1.944).toFixed(1);
      const time = this.getFormattedTimeFromData(data);
    const segmentInfo = data.segmentIndex !== undefined ? `Segment ${data.segmentIndex + 1}` : '';

    const windColor = this.getWindColorFromSpeed(speed);
    const intensityText = this.getWindIntensityText(beaufortScale);
    
    return `
      <div class="tooltip-header">
        <div class="tooltip-icon">💨</div>
        <div class="tooltip-title">Wind Information</div>
      </div>
      <div class="tooltip-content">
        <div class="tooltip-row">
          <span class="tooltip-label">Speed:</span>
          <span class="tooltip-value">
            <span class="tooltip-speed-indicator" style="background-color: ${windColor}"></span>
            ${speed.toFixed(1)} m/s (${speedKnots} kts)
          </span>
        </div>
        <div class="tooltip-row">
          <span class="tooltip-label">Direction:</span>
          <span class="tooltip-value">From ${cardinalDir} ${Math.round(normalizedFromDirection)}°</span>
        </div>
        <div class="tooltip-row" >
          <span class="tooltip-label">Condition:</span>
            <span class="tooltip-badge" style="background-color: ${windColor}; color: ${this.getContrastColor(windColor)};">
             ${beaufortScale} ${beaufortDescription}
            </span>
        </div>
        <div class="tooltip-time">${time}</div>
      </div>
    `;
  }

  // Wind-specific methods
  public convertWindSpeedToBeaufort(windSpeedMs: number): number {
    if (windSpeedMs < 0.5) return 0;
    if (windSpeedMs < 1.5) return 1;
    if (windSpeedMs < 3.3) return 2;
    if (windSpeedMs < 5.5) return 3;
    if (windSpeedMs < 7.9) return 4;
    if (windSpeedMs < 10.7) return 5;
    if (windSpeedMs < 13.8) return 6;
    if (windSpeedMs < 17.1) return 7;
    if (windSpeedMs < 20.7) return 8;
    if (windSpeedMs < 24.4) return 9;
    if (windSpeedMs < 28.4) return 10;
    if (windSpeedMs < 32.6) return 11;
    return 12;
  }

  private getBeaufortDescription(scale: number): string {
    const descriptions = [
      'Calm', 'Light air', 'Light breeze', 'Gentle breeze',
      'Moderate breeze', 'Fresh breeze', 'Strong breeze', 'High wind',
      'Gale', 'Strong gale', 'Storm', 'Violent storm', 'Hurricane'
    ];
    return descriptions[scale] || 'Unknown';
  }

  private getWindIntensityText(scale: number): string {
    if (scale <= 2) return 'LIGHT';
    if (scale <= 4) return 'MODERATE';
    if (scale <= 6) return 'STRONG';
    if (scale <= 8) return 'SEVERE';
    return 'EXTREME';
  }

   private getWindColorFromSpeed(speed: number): string {
    const scale = this.convertWindSpeedToBeaufort(speed);
    switch (scale) {
      case 0: return '#E3F2FD';  // Calm
      case 1: return '#E3F2FD';  // Light air
      case 2: return '#90CAF9';  // Light breeze
      case 3: return '#42A5F5';  // Gentle breeze
      case 4: return '#2196F3';  // Moderate breeze
      case 5: return '#1976D2';  // Fresh breeze
      case 6: return '#FF9800';  // Strong breeze
      case 7: return '#FF5722';  // High wind
      case 8: return '#F44336';  // Gale
      case 9: return '#E91E63';  // Strong gale
      case 10: return '#9C27B0'; // Storm
      case 11: return '#4A148C'; // Violent storm
      case 12: return '#311B92'; // Hurricane - even darker purple
      default: return '#000000'; // Fallback (black)
    }
}
}