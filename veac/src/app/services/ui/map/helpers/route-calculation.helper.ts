import { Coordinate } from 'ol/coordinate';
import { toLonLat } from 'ol/proj';
import { getDistance } from 'ol/sphere';
import { ClosestPointInfo } from '../voyage-map-service-type';

export class RouteCalculationHelper {
  
  static findClosestPointOnRoute(targetCoordinate: Coordinate, routeCoordinates: Coordinate[]): ClosestPointInfo | null {
    if (!routeCoordinates.length) return null;

    let minDistance = Infinity;
    let closestPoint: Coordinate | null = null;
    let closestSegmentIndex = 0;
    let timeRatio = 0;

    for (let i = 0; i < routeCoordinates.length - 1; i++) {
      const segmentStart = routeCoordinates[i];
      const segmentEnd = routeCoordinates[i + 1];
      
      const closestOnSegment = this.getClosestPointOnLineSegment(
        targetCoordinate, segmentStart, segmentEnd
      );
      
      const distance = getDistance(
        toLonLat(targetCoordinate),
        toLonLat(closestOnSegment.point)
      );
      
      if (distance < minDistance) {
        minDistance = distance;
        closestPoint = closestOnSegment.point;
        closestSegmentIndex = i;
        timeRatio = closestOnSegment.ratio;
      }
    }

    return closestPoint ? {
      coordinate: closestPoint,
      segmentIndex: closestSegmentIndex,
      timeRatio
    } : null;
  }

  private static getClosestPointOnLineSegment(
    point: Coordinate, 
    lineStart: Coordinate, 
    lineEnd: Coordinate
  ): { point: Coordinate; ratio: number } {
    const [px, py] = point;
    const [x1, y1] = lineStart;
    const [x2, y2] = lineEnd;
    
    const dx = x2 - x1;
    const dy = y2 - y1;
    
    if (dx === 0 && dy === 0) {
      return { point: lineStart, ratio: 0 };
    }
    
    const t = Math.max(0, Math.min(1, 
      ((px - x1) * dx + (py - y1) * dy) / (dx * dx + dy * dy)
    ));
    
    return {
      point: [x1 + t * dx, y1 + t * dy],
      ratio: t
    };
  }
  
  static getCardinalDirection(degrees: number): string {
    const normalizedDegrees = ((degrees % 360) + 360) % 360;
    const directions = ['N', 'NNE', 'NE', 'ENE', 'E', 'ESE', 'SE', 'SSE', 'S', 'SSW', 'SW', 'WSW', 'W', 'WNW', 'NW', 'NNW'];
    const index = Math.round(normalizedDegrees / 22.5) % 16;
    return directions[index];
  }
}