import { Map } from 'ol';
import { Feature } from 'ol';
import { Coordinate } from 'ol/coordinate';
import VectorSource from 'ol/source/Vector';

export class MapInteractionHelper {
  
  /**
   * Sets up all mouse and touch interactions for the map
   */
  static setupMapInteractions(
    map: Map,
    pointSource: VectorSource,
    onMapClick: (coordinate: Coordinate) => void,
    onDragStart: (feature: Feature) => void,
    onDragMove: (coordinate: Coordinate) => void,
    onDragEnd: () => void,
    isInteractionAllowed: () => boolean
  ): void {
    // Map click interaction
    map.on('click', (event: any) => {
      if (!isInteractionAllowed()) {
        return;
      }
      onMapClick(event.coordinate);
    });

    const mapElement = map.getTargetElement();
    if (!mapElement) return;

    this.setupMouseEvents(mapElement, map, pointSource, onDragStart, onDragMove, onDragEnd, isInteractionAllowed);
    this.setupTouchEvents(mapElement, map, pointSource, onDragStart, onDragMove, onDragEnd, isInteractionAllowed);
  }

  /**
   * Sets up mouse events for drag and drop functionality
   */
  private static setupMouseEvents(
    mapElement: HTMLElement,
    map: Map,
    pointSource: VectorSource,
    onDragStart: (feature: Feature) => void,
    onDragMove: (coordinate: Coordinate) => void,
    onDragEnd: () => void,
    isInteractionAllowed: () => boolean
  ): void {
    let isDragging = false;

    mapElement.addEventListener('mousedown', (event: MouseEvent) => {
      if (!isInteractionAllowed()) return;
      
      const pixel = map.getEventPixel(event) as [number, number];
      const pointFeature = this.findPointFeatureAtPixel(map, pixel, pointSource);
      
      if (pointFeature) {
        isDragging = true;
        onDragStart(pointFeature);
        event.preventDefault();
        event.stopPropagation();
      }
    });

    mapElement.addEventListener('mousemove', (event: MouseEvent) => {
      const pixel = map.getEventPixel(event) as [number, number];
      
      if (isDragging) {
        const coordinate = map.getCoordinateFromPixel(pixel);
        if (coordinate) {
          onDragMove(coordinate);
          event.preventDefault();
          event.stopPropagation();
        } 
      } else {
        this.updateCursor(mapElement, map, pixel, pointSource, isInteractionAllowed);
      }
    });

    mapElement.addEventListener('mouseup', (event: MouseEvent) => {
      if (isDragging) {
        isDragging = false;
        onDragEnd();
        event.preventDefault();
        event.stopPropagation();
      }
    });

    mapElement.addEventListener('mouseleave', () => {
      if (isDragging) {
        isDragging = false;
        onDragEnd();
      }
    });
  }

  /**
   * Sets up touch events for mobile drag and drop
   */
  private static setupTouchEvents(
    mapElement: HTMLElement,
    map: Map,
    pointSource: VectorSource,
    onDragStart: (feature: Feature) => void,
    onDragMove: (coordinate: Coordinate) => void,
    onDragEnd: () => void,
    isInteractionAllowed: () => boolean
  ): void {
    let isDragging = false;

    mapElement.addEventListener('touchstart', (event: TouchEvent) => {
      if (!isInteractionAllowed()) return;
      
      if (event.touches.length === 1) {
        const touch = event.touches[0];
        const pixel = this.getTouchPixel(touch, mapElement);
        const pointFeature = this.findPointFeatureAtPixel(map, pixel, pointSource);
        
        if (pointFeature) {
          isDragging = true;
          onDragStart(pointFeature);
          event.preventDefault();
        }
      }
    });

    mapElement.addEventListener('touchmove', (event: TouchEvent) => {
      if (isDragging && event.touches.length === 1) {
        const touch = event.touches[0];
        const pixel = this.getTouchPixel(touch, mapElement);
        const coordinate = map.getCoordinateFromPixel(pixel);
        
        if (coordinate) {
          onDragMove(coordinate);
          event.preventDefault();
        }
      }
    });

    mapElement.addEventListener('touchend', () => {
      if (isDragging) {
        isDragging = false;
        onDragEnd();
      }
    });
  }

  /**
   * Finds point features at a specific pixel
   */
  private static findPointFeatureAtPixel(map: Map, pixel: [number, number], pointSource: VectorSource): Feature | null {
    const features = map.getFeaturesAtPixel(pixel);
    const pointSourceFeatures = pointSource.getFeatures();
    const pointFeature = features.find((f: any) => {
      const isPointFeature = pointSourceFeatures.includes(f as Feature);
      return isPointFeature;
    });
    
    return pointFeature ? pointFeature as Feature : null;
  }

  /**
   * Converts touch coordinates to pixel coordinates
   */
  private static getTouchPixel(touch: Touch, mapElement: HTMLElement): [number, number] {
    const rect = mapElement.getBoundingClientRect();
    return [
      touch.clientX - rect.left,
      touch.clientY - rect.top
    ];
  }

  /**
   * Updates cursor based on hover state
   */
  private static updateCursor(
    mapElement: HTMLElement,
    map: Map,
    pixel: [number, number],
    pointSource: VectorSource,
    isInteractionAllowed: () => boolean
  ): void {
    const features = map.getFeaturesAtPixel(pixel);
    const hasPointFeature = features.some((f: any) => 
      pointSource.getFeatures().includes(f as Feature)
    );
    
    if (hasPointFeature) {
      mapElement.style.cursor = isInteractionAllowed() ? 'grab' : 'not-allowed';
    } else {
      mapElement.style.cursor = 'default';
    }
  }

  /**
   * Manages cursor states during drag operations
   */
  static setDragCursor(mapElement: HTMLElement | null): void {
    if (mapElement) {
      mapElement.style.cursor = 'grabbing';
    }
  }

  static resetCursor(mapElement: HTMLElement | null): void {
    if (mapElement) {
      mapElement.style.cursor = 'default';
    }
  }

  /**
   * Manages map panning during drag operations
   */
  static disableMapPanningDuringDrag(map: Map, disable: boolean): void {
    const interactions = map.getInteractions();
    
    interactions.forEach((interaction: any) => {
      const couldBePanInteraction = (
        interaction.setActive &&
        typeof interaction.setActive === 'function' &&
        interaction.getActive &&
        typeof interaction.getActive === 'function' &&
        (
          typeof interaction.handleDownEvent === 'function' ||
          typeof interaction.handleDragEvent === 'function' ||
          typeof interaction.handleMoveEvent === 'function' ||
          typeof interaction.handleUpEvent === 'function'
        )
      );
      
      if (couldBePanInteraction) {
        if (disable) {
          const wasActive = interaction.getActive();
          (interaction as any)._wasActiveBeforeDrag = wasActive;
          interaction.setActive(false);
        } else {
          const wasActive = (interaction as any)._wasActiveBeforeDrag !== undefined 
            ? (interaction as any)._wasActiveBeforeDrag 
            : true;
          interaction.setActive(wasActive);
          delete (interaction as any)._wasActiveBeforeDrag;
        }
      }
    });
  }
}