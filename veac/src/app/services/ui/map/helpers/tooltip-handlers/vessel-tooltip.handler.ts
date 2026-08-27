import { TooltipData } from './map-tooltip.helper';
import { BaseTooltipHandler } from './base-tooltip.handler';

export class VesselTooltipHandler extends BaseTooltipHandler {
  
  generateTooltip(data: TooltipData): string {
    const course = data.data.course || 0;
    const cardinalDir = this.getCardinalDirection(course);
    const time = this.getFormattedTimeFromData(data);
    const segmentInfo = data.segmentIndex !== undefined ? `Segment ${data.segmentIndex + 1}` : '';

    return `
      <div class="tooltip-header">
        <div class="tooltip-icon">🧭</div>
        <div class="tooltip-title">Vessel Course</div>
      </div>
      <div class="tooltip-content">
        <div class="tooltip-row">
          <span class="tooltip-label">Heading:</span>
          <span class="tooltip-value">${Math.round(course)}° ${cardinalDir}</span>
        </div>
        <div class="tooltip-time">${time}</div>
      </div>
    `;
  }
}