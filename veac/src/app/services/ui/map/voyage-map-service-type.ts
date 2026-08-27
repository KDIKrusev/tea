import { Weather } from '../../../models/entities/weather.model';
import VectorLayer from 'ol/layer/Vector';
import VectorSource from 'ol/source/Vector';
import { Coordinate } from 'ol/coordinate';
import { Feature } from 'ol';

export const MAP_CONFIG = {
  INITIAL_COORDS: [0, 30] as [number, number],
  MINI_MAP_ZOOM: 3,
  DEFAULT_ZOOM: 2,
  MAX_ZOOM: 18,
  ROUTE_STROKE_WIDTH: 4,
  ROUTE_COLOR: '#3498db',
  INTERACTIVE_STROKE_WIDTH: 25,
  POINT_RADIUS: 12,
  POINT_COLOR: '#FF6B6B',
  POINT_BORDER_COLOR: '#424242',
  POINT_BORDER_WIDTH: 3,
  DRAG_THROTTLE_MS: 100,
  MAP_PADDING: [50, 50, 50, 50] as number[],
  
  // Unified overlay configuration
   OVERLAY: {
    ARROW_LENGTH: 0.12,
    ARROW_SIZE: 20,
    ICON_SIZE: 28,
    LABEL_OFFSET: -45,
    OFFSETS: {
      VESSEL: { x: 0, y: 0 },
      WIND: { x: 0.25, y: 0 },
      CURRENT: { x: -0.25, y: 0 },
      WEATHER: { x: 0, y: 0.25 },
      WAVES: { x: 800, y: -600 }
    },
    COLORS: {
      VESSEL: '#E74C3C',
      WIND: '#2980B9',
      CURRENT: '#27AE60',
      WEATHER: '#F39C12',
      WAVES: '#3498DB'
    }
  }
} as const;

// Simplified overlay types
export type OverlayType = 'vessel' | 'wind' | 'current' | 'weather' | 'waves';
export type VisualType = 'connection' | 'line' | 'arrow' | 'label' | 'point';

// Base vector data interface
export interface VectorData {
  direction?: number;
  speed?: number;
  height?: number;   
  period?: number;
  course?: number;
  weather?: Weather;
  segmentIndex: number;
  avgNetWeatherResistancePower?: number;
  favorableWeatherIndex?: number;
  avgTotalResistanceFuelConsumption?: number;
}

// Backward compatible overlay settings
export interface MapOverlaySettings {
  activeOverlay: OverlayType | 'none';
  showLabels: boolean;
  vesselCourse: boolean;
  trueWind: boolean;
  trueCurrent: boolean;
  weatherData: boolean;
  trueWaves: boolean;
  routeSegments: boolean;
}

// Legacy interfaces for backward compatibility
export interface MapLayers {
  route: VectorLayer<VectorSource>;
  interactive: VectorLayer<VectorSource>;
  point: VectorLayer<VectorSource>;
  overlay: VectorLayer<VectorSource>;
  // Legacy layer properties for backward compatibility
  vesselCourse?: VectorLayer<VectorSource>;
  windVectors?: VectorLayer<VectorSource>;
  currentVectors?: VectorLayer<VectorSource>;
  weatherPoints?: VectorLayer<VectorSource>;
}

export interface MapSources {
  route: VectorSource;
  interactive: VectorSource;
  point: VectorSource;
  overlay: VectorSource;
}

export interface InteractionState {
  isDragging: boolean;
  dragFeature: Feature | null;
  lastDragUpdate: number;
}

export interface ClosestPointInfo {
  coordinate: Coordinate;
  segmentIndex: number;
  timeRatio: number;
}

export interface OverlayVisualizationMode {
  vesselCourse: 'simplified';
  trueWind: 'vectors';
  trueCurrent: 'vectors';
  trueWaves: 'vectors'
}
export interface StyleConfig {
  color: string;
  size: number;
  icon: string;
  label: string;
}