import { TooltipData } from './map-tooltip.helper';
import { RouteSegment } from '../../../../../models/entities/route-segment.model';

export abstract class BaseTooltipHandler {
  
  abstract generateTooltip(data: TooltipData): string;

  // Common utility methods
  protected getCardinalDirection(degrees: number): string {
    const normalizedDegrees = ((degrees % 360) + 360) % 360;
    const directions = ['N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE', 'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW'];
    const index = Math.round(normalizedDegrees / 22.5) % 16;
    return directions[index];
  }

  protected formatTime(startTime?: Date, endTime?: Date): string {
  if (!startTime) return 'N/A';
  
  const formatOptions: Intl.DateTimeFormatOptions = {
    hour: '2-digit',
    minute: '2-digit',
    day: '2-digit',
    month: '2-digit',
    year: '2-digit',
    hour12: false,
    timeZone: 'UTC'
  };
  
  const startFormatted = startTime.toLocaleString('en-GB', formatOptions);
  
  if (endTime) {
    const endFormatted = endTime.toLocaleString('en-GB', formatOptions);
    
    // Check if dates are the same
    const startDateOnly = startTime.toLocaleDateString('en-GB', { 
      day: '2-digit', 
      month: '2-digit', 
      year: '2-digit',
      timeZone: 'UTC'  
    });
    const endDateOnly = endTime.toLocaleDateString('en-GB', { 
      day: '2-digit', 
      month: '2-digit', 
      year: '2-digit',
      timeZone: 'UTC' 
    });
    
    if (startDateOnly === endDateOnly) {
      // Same date - show: "22:33 - 22:48 (25/05/25)"
      const startTimeOnly = startTime.toLocaleTimeString('en-GB', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
        timeZone: 'UTC' 
      });
      const endTimeOnly = endTime.toLocaleTimeString('en-GB', {
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
        timeZone: 'UTC' 
      });
      
      return `${startTimeOnly} - ${endTimeOnly} (${startDateOnly}) `;
    } else {
      // Different dates - show full timestamps
      return `${startFormatted} - ${endFormatted} `;
    }
  }
  
  return `${startFormatted} `;
}

  // Add time extraction method to base class
  protected getTimeFromSegment(segmentIndex: number, routeSegments?: RouteSegment[]): { startTime?: Date, endTime?: Date } {
    if (!routeSegments || segmentIndex >= routeSegments.length) {
      return {};
    }

    const segment = routeSegments[segmentIndex];
    if (!segment) {
      return {};
    }

    let startTime: Date | undefined;
    let endTime: Date | undefined;

    // Convert startTime (already in milliseconds)
    if (segment.startTime) {
      startTime = new Date(segment.startTime); // Remove * 1000 since it's already in milliseconds
    }

    // Convert endTime if available
    if (segment.endTime) {
      endTime = new Date(segment.endTime);
    } else if (segmentIndex < routeSegments.length - 1) {
      // Use next segment's startTime as this segment's endTime
      const nextSegment = routeSegments[segmentIndex + 1];
      if (nextSegment?.startTime) {
        endTime = new Date(nextSegment.startTime);
      }
    }

    return { startTime, endTime };
  }

  // Helper method to get formatted time string from TooltipData
  protected getFormattedTimeFromData(data: TooltipData): string {
    // First try to use the provided start/end times
    if (data.startTime || data.endTime) {
      return this.formatTime(data.startTime, data.endTime);
    }
    
    // Fallback to extracting from route segments
    if (data.segmentIndex !== undefined && data.routeSegments) {
      const timeData = this.getTimeFromSegment(data.segmentIndex, data.routeSegments);
      return this.formatTime(timeData.startTime, timeData.endTime);
    }
    
    return 'N/A';
  }

  protected getContrastColor(backgroundColor: string): string {
    const lightColors = [
      '#E3F2FD', '#90CAF9', '#42A5F5', '#F3E5F5', '#CE93D8',
      '#E8F5E8', '#C8E6C9', '#81C784'
    ];
    const darkColors = [
      '#2196F3', '#1976D2', '#FF9800', '#FF5722', '#F44336',
      '#E91E63', '#9C27B0', '#4A148C', '#AB47BC', '#8E24AA',
      '#6A1B9A', '#4CAF50', '#2E7D32'
    ];
    
    if (lightColors.includes(backgroundColor)) {
      return 'black';
    } else if (darkColors.includes(backgroundColor)) {
      return 'white';
    } else {
      return 'white';
    }
  }
}