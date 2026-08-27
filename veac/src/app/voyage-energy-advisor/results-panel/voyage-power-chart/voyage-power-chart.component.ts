import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, OnDestroy, ViewChild, ElementRef, AfterViewInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import * as d3 from 'd3';
import { VoyageOption } from '../../../models/entities/voyage-option.model';
import { RouteSegment } from '../../../models/entities/route-segment.model';
import { fromEvent, Subject, Subscription } from 'rxjs';
import { debounceTime, takeUntil } from 'rxjs/operators';
import { VoyageService, SegmentSelection, DisplayFormat } from '../../../services/state/voyage-scheduler.service'
import { PowerChartService } from '../../../services/utilities/power-chart.service';

interface PowerDataItem {
  label: string;
  value: number;      
  valueDisplay?: number;
  percentage?: number | null; 
  color: string;
}

@Component({
  selector: 'app-voyage-power-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './voyage-power-chart.component.html',
  styleUrls: ['./voyage-power-chart.component.css']
})

export class VoyagePowerChartComponent implements OnChanges, OnDestroy, AfterViewInit {
  @ViewChild('chartContainer', { static: true }) chartContainer!: ElementRef<HTMLDivElement>;
  @ViewChild('powerChartContainer', { static: false, read: ElementRef }) powerChartContainer?: ElementRef<HTMLDivElement>;
  @Input() selectedVoyageOption!: VoyageOption;
  @Output() segmentSelected = new EventEmitter<RouteSegment>();
  @Output() routeSegmentsReady = new EventEmitter<RouteSegment[]>(); // Add this output for map

  public currentDisplayFormat: DisplayFormat = 'energy';

  constructor(
      private voyageSchedulerService: VoyageService,
      private powerChartService: PowerChartService) 
  {}
  
  private svg!: d3.Selection<SVGSVGElement, unknown, null, undefined>;
  private powerChartSvg!: d3.Selection<SVGSVGElement, unknown, null, undefined>;
  private width = 0;
  private height = 0;
  private margin = { top: 10, right: 20, bottom: 45, left: 60 };
  private xScale!: d3.ScaleTime<number, number>;
  private yScale!: d3.ScaleLinear<number, number>;
  private customTicks: number[] = [];
  private timeExtent: [Date, Date] = [new Date(), new Date()];
  
  // Enhanced interaction state
  public selectedSegment: RouteSegment | null = null;
  public currentSegmentIndex = 0;
  public selectorLinePosition = 0;
  public showAdditionalPanel = true;
  
  // Selector line element
  private selectorLine: d3.Selection<SVGLineElement, unknown, null, undefined> | null = null;
  private subscriptions: Subscription[] = [];
  
  private destroy$ = new Subject<void>();

  ngOnInit(): void {
   const displayFormatSub = this.voyageSchedulerService.displayFormat$.subscribe(format => {
      this.currentDisplayFormat = format;
      this.updateChart();
      if (this.showAdditionalPanel && this.selectedSegment) {
        setTimeout(() => this.updatePowerChart(), 100);
      }
    });

  const toggleSub = this.voyageSchedulerService.showFuelConsumption$.subscribe(() => {
    this.updateChart();
    if (this.showAdditionalPanel && this.selectedSegment) {
      setTimeout(() => this.updatePowerChart(), 100);
    }
  });
  this.subscriptions.push(toggleSub, displayFormatSub);
}

  ngAfterViewInit(): void {
    this.initChart();
    
    fromEvent(window, 'resize')
      .pipe(debounceTime(150), takeUntil(this.destroy$))
      .subscribe(() => this.handleResize());

    this.voyageSchedulerService.selectedSegment$
      .pipe(takeUntil(this.destroy$))
      .subscribe((selection: SegmentSelection | null) => {
        if (selection && this.selectedVoyageOption?.routeSegments) {
          this.handleSegmentSelectionFromMap(selection);
        }
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedVoyageOption'] && this.selectedVoyageOption) {
      setTimeout(() => {
        this.updateChart();
        this.initializeSelector();
        
        if (this.selectedVoyageOption.routeSegments) {
          this.routeSegmentsReady.emit(this.selectedVoyageOption.routeSegments);
        }
        
        if (this.showAdditionalPanel) {
          setTimeout(() => {
            this.initPowerChart();
          }, 100);
        }
      }, 0);
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  public get showFuelConsumption(): boolean {
    return this.currentDisplayFormat === 'fuel';
  }

  public get showCost(): boolean {
    return this.currentDisplayFormat === 'cost';
  }

  private handleSegmentSelectionFromMap(selection: SegmentSelection): void {
    if (!this.selectedVoyageOption?.routeSegments) return;

    const segment = this.selectedVoyageOption.routeSegments[selection.segmentIndex];
    if (!segment) {
      console.warn('📊 Chart: Segment not found at index:', selection.segmentIndex);
      return;
    }

    // Update current state without triggering a loop
    this.currentSegmentIndex = selection.segmentIndex;
    this.selectedSegment = segment;
    this.segmentSelected.emit(segment);

    this.updateSelectorLinePosition(segment, selection.timeWithinSegment);

    if (this.showAdditionalPanel) {
      setTimeout(() => this.updatePowerChart(), 100);
    }

  }

  private updateSelectorLinePosition(segment: RouteSegment, preciseTime?: number): void {
  if (!this.xScale) {
    console.error("❌ No xScale available");
    return;
  }

  let targetTime: Date;
  let timeSource: string;

  if (preciseTime && preciseTime >= segment.startTime && preciseTime <= segment.endTime) {
    // Use precise time if it's within the segment bounds
    targetTime = new Date(preciseTime);
    timeSource = 'precise';
  } else {
    // Always fall back to segment midpoint for consistency
    const midTime = (segment.startTime + segment.endTime) / 2;
    targetTime = new Date(midTime);
    timeSource = 'midpoint';
    
    if (preciseTime) {
      console.warn('📊 Chart: Precise time', new Date(preciseTime), 
                   'is outside segment bounds [', new Date(segment.startTime), 
                   '-', new Date(segment.endTime), '], using midpoint');
    }
  }

  this.selectorLinePosition = this.xScale(targetTime);
  if (!Number.isFinite(this.selectorLinePosition)) {
    return;
  }

  this.updateSelectorLine();
}


  // Keyboard navigation for selector line
  @HostListener('document:keydown', ['$event'])
  handleKeyDown(event: KeyboardEvent): void {
    if (!this.selectedVoyageOption?.routeSegments) return;

    if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
      event.preventDefault();
      
      const direction = event.key === 'ArrowLeft' ? -1 : 1;
      const newIndex = Math.max(0, Math.min(
        this.selectedVoyageOption.routeSegments.length - 1,
        this.currentSegmentIndex + direction
      ));
      
      if (newIndex !== this.currentSegmentIndex) {
        this.currentSegmentIndex = newIndex;
        this.selectSegmentByIndex(newIndex);
      }
    }
  }

  // Initialize selector at start of voyage
  private initializeSelector(): void {
  if (this.selectedVoyageOption?.routeSegments?.length > 0) {
    if (this.selectedSegment === null || this.currentSegmentIndex < 0) {
      this.currentSegmentIndex = 0;
      this.selectSegmentByIndex(0);
    } else {
      const maxIndex = this.selectedVoyageOption.routeSegments.length - 1;
      if (this.currentSegmentIndex > maxIndex) {
        this.currentSegmentIndex = maxIndex;
      }
      // Re-select the current segment to update the display
      this.selectSegmentByIndex(this.currentSegmentIndex);
    }
  }
}

  private selectSegmentByIndex(index: number): void {
  
  if (!this.selectedVoyageOption?.routeSegments?.[index]) {
    console.error("❌ Invalid segment index or no segments");
    return;
  }

  const segment = this.selectedVoyageOption.routeSegments[index];
  
  this.selectedSegment = segment;
  this.segmentSelected.emit(segment);

  // Extract coordinates from segment
  let lat: number | undefined;
  let lng: number | undefined;

  if (segment.startPosition) {
    lat = segment.startPosition.latitude;
    lng = segment.startPosition.longitude;
  } else if (segment.endPosition) {
    lat = segment.endPosition.latitude;
    lng = segment.endPosition.longitude;
  }

  // Calculate midpoint time for segment
  const midTime = (segment.startTime + segment.endTime) / 2;

  // Send selection to service (this will update the map)
  this.voyageSchedulerService.setSelectedSegmentIndex(index, lat, lng, midTime);

  // UPDATED: Use centralized positioning method
  this.updateSelectorLinePosition(segment);

  if (this.showAdditionalPanel) {
    setTimeout(() => this.updatePowerChart(), 100);
  }
  
}

   public getChartTitle(): string {
    switch (this.currentDisplayFormat) {
      case 'cost': return 'Cost breakdown';
      case 'fuel': return 'Fuel consumption';
      default: return 'Power requirements';
    }
  }

public getCurrentDisplayValue(): string {
    if (!this.selectedSegment) return '';
    
    const segment = this.selectedSegment as any;
    
    if (this.currentDisplayFormat === 'cost') {
      // Use backend-calculated cost value
      const totalCost = segment.avgTotalResistanceCost || 0;
      return `$${(totalCost / 1000).toFixed(2)}K`;
    } else if (this.currentDisplayFormat === 'fuel') {
      return `${(segment.avgTotalResistanceFuelConsumption / 1000).toFixed(2)} MT/h`;
    } else {
      return `${(segment.avgTotalPower / 1000).toFixed(2)} MW`;
    }
  }

  // Update selector line position
  private updateSelectorLine(): void {
    if (!this.selectorLine) {
      console.warn('📊 Chart: Selector line not available');
      return;
    }

    if (!Number.isFinite(this.selectorLinePosition)) {
      return;
    }

    this.selectorLine
      .attr('x1', this.selectorLinePosition)
      .attr('x2', this.selectorLinePosition)
      .attr('y1', 0)
      .attr('y2', this.height)
      .style('display', 'block');
  }

  private initChart(): void {
    const container = this.chartContainer.nativeElement;
    
    this.width = container.offsetWidth - this.margin.left - this.margin.right;
    this.height = Math.max(180, container.offsetHeight - 100) - this.margin.top - this.margin.bottom;
    
    // Clear any existing SVG
    d3.select(container.querySelector('.chart-svg-container')).selectAll('*').remove();

    // Create SVG
    this.svg = d3.select(container.querySelector('.chart-svg-container'))
      .append('svg')
      .attr('width', '100%')
      .attr('height', this.height + this.margin.top + this.margin.bottom)
      .attr('viewBox', `0 0 ${this.width + this.margin.left + this.margin.right} ${this.height + this.margin.top + this.margin.bottom}`)
      .attr('preserveAspectRatio', 'xMinYMin meet')
      .attr('class', 'compact-chart-svg')
      .style('overflow', 'visible');

    // Add background
    this.svg.append('rect')
      .attr('width', this.width + this.margin.left + this.margin.right)
      .attr('height', this.height + this.margin.top + this.margin.bottom)
      .attr('fill', '#f8fafc')
      .attr('rx', 8);

    // Chart group
    const chartGroup = this.svg.append('g')
      .attr('transform', `translate(${this.margin.left},${this.margin.top})`);

    // Setup scales
    this.setupScales();
    
    // Create selector line (initially hidden)
    this.selectorLine = chartGroup.append('line')
      .attr('class', 'selector-line')
      .attr('y1', 0)
      .attr('y2', this.height)
      .attr('stroke', '#FF6B6B')
      .attr('stroke-width', 2)
      .style('display', 'none');
  }

  get isVesselNotMoving(): boolean {
  return this.selectedVoyageOption && 
         (!this.selectedVoyageOption.routeSegments || 
          this.selectedVoyageOption.routeSegments.length === 0);
}

  private setupScales(): void {
    this.xScale = d3.scaleTime().range([0, this.width]);
    this.yScale = d3.scaleLinear().range([this.height, 0]);
  }

  private updateChart(): void {
   if (!this.selectedVoyageOption?.routeSegments || !this.svg) return;

    const segments = this.selectedVoyageOption.routeSegments;
    
    // Prepare data
    const data = segments.map(segment => {
      let totalValue: number;
      let calmWaterValue: number;

      if (this.currentDisplayFormat === 'cost') {
        // Use backend-calculated cost values
        totalValue = segment.avgTotalResistanceCost || 0;
        calmWaterValue = segment.avgCalmWaterResistanceCost || 0;
      } else if (this.currentDisplayFormat === 'fuel') {
        totalValue = segment.avgTotalResistanceFuelConsumption || 0;
        calmWaterValue = segment.avgCalmWaterResistanceFuelConsumption || 0;
      } else {
        totalValue = segment.avgTotalPower || 0;
        calmWaterValue = segment.avgCalmWaterPower || 0;
      }

      return {
        startTime: new Date(segment.startTime),
        endTime: new Date(segment.endTime),
        totalPower: totalValue,
        calmWater: calmWaterValue,
        segment: segment
      };
    }).filter(d =>
      Number.isFinite(d.startTime.getTime()) &&
      Number.isFinite(d.endTime.getTime()) &&
      Number.isFinite(d.totalPower) &&
      Number.isFinite(d.calmWater)
    );

    if (!data.length) {
      return;
    }

    const timeExtent = d3.extent([
      ...data.map(d => d.startTime),
      ...data.map(d => d.endTime)
    ]) as [Date, Date];
    
    this.timeExtent = timeExtent;
    
    const allPowerValues = [
      ...data.map(d => d.totalPower),
      ...data.map(d => d.calmWater)
    ];

    
    const minValue = Math.min(...allPowerValues);
    const maxValue = Math.max(...allPowerValues);
    const range = maxValue - minValue;
    const padding = range === 0 ? Math.max(Math.abs(maxValue) * 0.05, 1) : range * 0.05;
    
    const yMin = minValue - padding;
    const yMax = maxValue + padding;
    
    // Create custom tick values
    const tickCount = 5;
    const tickStep = (yMax - yMin) / (tickCount - 1);
    this.customTicks = [];
    for (let i = 0; i < tickCount; i++) {
      this.customTicks.push(yMin + (i * tickStep));
    }

    this.xScale.domain(timeExtent);
    this.yScale.domain([yMin, yMax]);

    const chartGroup = this.svg.select('g');
    
    // Clear existing chart elements
    chartGroup.selectAll('.chart-element').remove();

    // Create step data
    const stepData = this.createStepData(data);
    
    // Add visualization
    this.addMinimalGrid(chartGroup);
    this.addCompactVisualization(chartGroup, stepData);
    this.addCompactAxes(chartGroup);
    this.addInteractionPoints(chartGroup, data);
    
    // Update selector line position after chart update
    if (this.selectedSegment && this.selectorLine) {
      this.selectorLine.attr('y2', this.height);
      this.updateSelectorLine();
    }
  }

  private createStepData(data: any[]): any[] {
    const stepData: any[] = [];
    
    data.forEach(d => {
      stepData.push({
        time: d.startTime,
        totalPower: d.totalPower,
        calmWater: d.calmWater,
        segment: d.segment
      });
      stepData.push({
        time: d.endTime,
        totalPower: d.totalPower,
        calmWater: d.calmWater,
        segment: d.segment
      });
    });
    
    return stepData;
  }

  private addMinimalGrid(chartGroup: any): void {
    chartGroup.selectAll('.grid-line')
      .data(this.customTicks)
      .enter()
      .append('line')
      .attr('class', 'chart-element grid-line')
      .attr('x1', 0)
      .attr('x2', this.width)
      .attr('y1', (d: number) => this.yScale(d))
      .attr('y2', (d: number) => this.yScale(d))
      .attr('stroke', '#e2e8f0')
      .attr('stroke-width', 1)
      .attr('opacity', 0.5);
  }

  private addCompactVisualization(chartGroup: any, stepData: any[]): void {
    this.createConditionalAreas(chartGroup, stepData);
    this.addConditionalPowerLines(chartGroup, stepData);
    
    // Base power line (calm water resistance)
    const baseLineGenerator = d3.line<any>()
      .x(d => this.xScale(d.time))
      .y(d => this.yScale(d.calmWater))
      .curve(d3.curveStepAfter);

    chartGroup.append('path')
      .datum(stepData)
      .attr('class', 'chart-element base-power-line')
      .attr('d', baseLineGenerator)
      .attr('fill', 'none')
      .attr('stroke', '#AAAAAA')
      .attr('stroke-width', 1.5)
      .attr('stroke-dasharray', '3,3');
  }

  private createConditionalAreas(chartGroup: any, stepData: any[]): void {
    for (let i = 0; i < stepData.length - 1; i++) {
      const current = stepData[i];
      const next = stepData[i + 1];
      
      if (!current || !next) continue;
      
      const isBelowLine = current.totalPower < current.calmWater;
      
      const areaGenerator = d3.area<any>()
        .x(d => this.xScale(d.time))
        .y0(d => this.yScale(d.calmWater))
        .y1(d => this.yScale(d.totalPower))
        .curve(d3.curveStepAfter);

      chartGroup.append('path')
        .datum([current, next])
        .attr('class', `chart-element segment-area ${isBelowLine ? 'below' : 'above'}`)
        .attr('d', areaGenerator)
        .attr('fill', isBelowLine ? '#4A72AA' : '#D3D3D3')
        .attr('fill-opacity', isBelowLine ? 0.1 : 0.2);
    }
  }

  private addConditionalPowerLines(chartGroup: any, stepData: any[]): void {
    for (let i = 0; i < stepData.length - 1; i++) {
      const current = stepData[i];
      const next = stepData[i + 1];
      
      if (!current || !next) continue;
      
      const isBelowLine = current.totalPower < current.calmWater;
      
      const lineGenerator = d3.line<any>()
        .x(d => this.xScale(d.time))
        .y(d => this.yScale(d.totalPower))
        .curve(d3.curveStepAfter);

      chartGroup.append('path')
        .datum([current, next])
        .attr('class', `chart-element power-line-segment ${isBelowLine ? 'below' : 'above'}`)
        .attr('d', lineGenerator)
        .attr('fill', 'none')
        .attr('stroke', isBelowLine ? '#4A72AA' : '#707070')
        .attr('stroke-width', 2)
        .attr('stroke-linecap', 'round');
    }
  }

  private addCompactAxes(chartGroup: any): void {
    // X axis with better spacing
    const xAxis = d3.axisBottom(this.xScale)
      .ticks(d3.timeHour.every(1))
      .tickFormat((d: any) => {
        const date = new Date(d);
        const day = String(date.getUTCDate()).padStart(2, '0');
        const month = String(date.getUTCMonth() + 1).padStart(2, '0');
        const hours = String(date.getUTCHours()).padStart(2, '0');
        const minutes = String(date.getUTCMinutes()).padStart(2, '0');
        return `${day}/${month} ${hours}:${minutes}`;
      })
      .tickSize(0);

    const startTime = this.timeExtent[0].getTime();
    const endTime = this.timeExtent[1].getTime();
    const duration = endTime - startTime;
    
    const tickTimes = [];
    for (let i = 0; i < 8; i++) {
      const tickTime = startTime + (duration * i / 7);
      tickTimes.push(new Date(tickTime));
    }
    
    xAxis.tickValues(tickTimes);

    const xAxisGroup = chartGroup.append('g')
      .attr('class', 'chart-element x-axis')
      .attr('transform', `translate(0,${this.height})`)
      .call(xAxis);
      
    xAxisGroup.selectAll('text')
      .style('font-size', '9px')
      .style('fill', '#64748b')
      .attr('dy', '1.5em')
      .style('text-anchor', 'middle');

    // Y axis
       const yAxis = d3.axisLeft(this.yScale)
      .tickValues(this.customTicks)
      .tickFormat((d: any) => {
        const value = d / 1000;
        let unit: string;
        
        if (this.currentDisplayFormat === 'cost') {
          return `$${value.toFixed(2)}K/h`;
        } else if (this.currentDisplayFormat === 'fuel') {
          unit = 't/h';
        } else {
          unit = 'MW';
        }
        
        return `${value.toFixed(2)} ${unit}`;
      })
      .tickSize(-this.width)
      .tickPadding(10);

    const yAxisGroup = chartGroup.append('g')
      .attr('class', 'chart-element y-axis')
      .call(yAxis);
    
    yAxisGroup.selectAll('text')
      .style('font-size', '11px')
      .style('fill', '#666666');
      
    yAxisGroup.selectAll('line')
      .style('stroke', '#e2e8f0')
      .style('stroke-width', '0.5px')
      .style('opacity', '0.5');

    chartGroup.selectAll('.domain').remove();
  }

  private addInteractionPoints(chartGroup: any, data: any[]): void {
  chartGroup.selectAll('.interaction-point')
    .data(data)
    .enter()
    .append('rect')
    .attr('class', 'chart-element interaction-point')
    .attr('x', (d: any) => this.xScale(d.startTime))
    .attr('y', 0)
    .attr('width', (d: any) => this.xScale(d.endTime) - this.xScale(d.startTime))
    .attr('height', this.height)
    .attr('fill', 'transparent')
    .attr('cursor', 'pointer')
    .on('click', (event: any, d: any) => {;
      
      // FIXED: Use a more reliable way to find segment index
      const segmentIndex = this.findSegmentIndex(d.segment);
      
      if (segmentIndex >= 0) {
        this.currentSegmentIndex = segmentIndex;
        
        this.selectSegmentByIndex(segmentIndex);
        
      } else {
        console.error("❌ Could not find segment index for clicked data");
      }
    });
}

private findSegmentIndex(targetSegment: RouteSegment): number {
  if (!this.selectedVoyageOption?.routeSegments) {
    console.error("No route segments available");
    return -1;
  }
  
  // Method 1: Try exact object reference match first
  let index = this.selectedVoyageOption.routeSegments.findIndex(s => s === targetSegment);
  
  if (index >= 0) {
    return index;
  }
  
  // Method 2: Find by unique properties if object reference fails
  index = this.selectedVoyageOption.routeSegments.findIndex(s => 
    s.startTime === targetSegment.startTime &&
    s.endTime === targetSegment.endTime &&
    s.course === targetSegment.course &&
    s.startPosition.latitude === targetSegment.startPosition.latitude &&
    s.startPosition.longitude === targetSegment.startPosition.longitude
  );
  
  if (index >= 0) {
    return index;
  }
  
  // Method 3: Find by start time (should be unique)
  index = this.selectedVoyageOption.routeSegments.findIndex(s => 
    s.startTime === targetSegment.startTime
  );
  
  if (index >= 0) {
    return index;
  }
  
  console.error("❌ Could not find segment by any method");
  console.error("Target segment:", targetSegment);
  console.error("Available segments:", this.selectedVoyageOption.routeSegments.map((s, i) => ({
    index: i,
    startTime: s.startTime,
    course: s.course
  })));
  
  return -1;
}

  private handleResize(): void {
    const container = this.chartContainer.nativeElement;
    
    const newWidth = container.offsetWidth - this.margin.left - this.margin.right;
    const newHeight = Math.max(180, container.offsetHeight - 100) - this.margin.top - this.margin.bottom;

    if (this.svg) {
      this.svg.remove();
    }
    this.width = newWidth;
    this.height = newHeight;
    this.initChart();
    
    if (this.selectedVoyageOption?.routeSegments) {
      this.updateChart();
      
      if (this.selectedSegment) {
        this.selectSegmentByIndex(this.currentSegmentIndex);
      }
    }

    if (this.showAdditionalPanel && this.powerChartContainer) {
      setTimeout(() => {
        this.updatePowerChart();
      }, 50);
    }
  }

  private initPowerChart(): void {
    if (!this.powerChartContainer?.nativeElement) {
      return;
    }

    const container = this.powerChartContainer.nativeElement;
    
    d3.select(container).selectAll('*').remove();
    container.innerHTML = '';

    const containerWidth = 360;
    const containerHeight = 300;

    const chartDiv = d3.select(container)
      .append('div')
      .style('width', '100%')
      .style('height', '100%')
      .style('background', '#F7F7F7')
      .style('padding', '12px')
      .style('position', 'relative')
      .style('box-sizing', 'border-box')
      .style('border-radius', '8px');

    const margin = { top: 20, right: 120, bottom: 20, left: 200 }; 
    const width = containerWidth - margin.left - margin.right;
    const height = containerHeight - margin.top - margin.bottom;

    this.powerChartSvg = chartDiv
      .append('svg')
      .attr('width', width + margin.left + margin.right)
      .attr('height', height + margin.top + margin.bottom)
      .style('background', '#F7F7F7');

    const chartGroup = this.powerChartSvg.append('g')
      .attr('transform', `translate(${margin.left},${margin.top})`);

    (this.powerChartSvg as any).width = width;
    (this.powerChartSvg as any).height = height;
    (this.powerChartSvg as any).margin = margin;
    (this.powerChartSvg as any).container = chartDiv;
    (this.powerChartSvg as any).chartGroup = chartGroup;
    
    if (this.selectedSegment) {
      setTimeout(() => this.updatePowerChart(), 50);
    }
  }
private updatePowerChart(): void {
    if (!this.powerChartSvg || !this.selectedSegment) return;

    const svg = this.powerChartSvg;
    const width = (svg as any).width;
    const height = (svg as any).height;
    const chartGroup = (svg as any).chartGroup;

    const data = this.powerChartService.getPowerData(this.selectedSegment, this.currentDisplayFormat);
    if (!data || data.length === 0) {
      console.warn('No power data available');
      return;
    }

    const yScale = d3.scaleBand()
      .domain(data.map((d: PowerDataItem) => d.label))
      .range([0, height])
      .padding(0.4);

    const maxAbs = d3.max(data, (d: PowerDataItem) => Math.abs(d.value)) || 1000;
    const minValue = d3.min(data, (d: PowerDataItem) => d.value) || 0;
    const maxValue = d3.max(data, (d: PowerDataItem) => d.value) || 1000;
    
    const zeroLinePosition = width * 0.3;
    const maxLeftValue = Math.abs(minValue) || maxAbs * 0.5;
    const maxRightValue = maxValue || maxAbs * 0.5;
    
    const leftPadding = maxLeftValue * 0.2;
    const rightPadding = maxRightValue * 0.2;
    
    const xScale = d3.scaleLinear()
      .domain([-(maxLeftValue + leftPadding), maxRightValue + rightPadding])
      .range([0, width]);

    chartGroup.selectAll('*').remove();

    // Add zero line
    chartGroup.append('line')
      .attr('class', 'zero-line')
      .attr('x1', zeroLinePosition)
      .attr('x2', zeroLinePosition)
      .attr('y1', -5)
      .attr('y2', height + 5)
      .attr('stroke', '#4b5563')
      .attr('stroke-width', 2)
      .style('opacity', 0.8);

    chartGroup.append('text')
      .attr('x', zeroLinePosition)
      .attr('y', -8)
      .attr('text-anchor', 'middle')
      .style('font-size', '10px')
      .style('font-weight', '600')
      .style('fill', '#4b5563')
      .text('0');

    chartGroup.append('text')
      .attr('x', zeroLinePosition - 25)
      .attr('y', -8)
      .attr('text-anchor', 'end')
      .style('font-size', '12px')
      .style('font-weight', '600')
      .style('fill', '#4b5563')
      .text('Contribution');

    chartGroup.append('text')
      .attr('x', zeroLinePosition + 25)
      .attr('y', -8)
      .attr('text-anchor', 'start')
      .style('font-size', '12px')
      .style('font-weight', '600')
      .style('fill', '#4b5563')
      .text('Resistance');

    const bars = chartGroup.selectAll('.bar')
      .data(data)
      .enter()
      .append('g')
      .attr('class', 'bar-group');

    bars.append('rect')
      .attr('class', 'bar')
      .attr('y', (d: PowerDataItem) => yScale(d.label)!)
      .attr('height', yScale.bandwidth())
      .attr('x', (d: PowerDataItem) => {
        if (d.value >= 0) {
          return zeroLinePosition;
        } else {
          return zeroLinePosition + xScale(d.value) - xScale(0);
        }
      })
      .attr('width', (d: PowerDataItem) => {
        return Math.abs(xScale(d.value) - xScale(0));
      })
      .attr('fill', (d: PowerDataItem) => this.getSimpleBarColor(d.value))
      .attr('rx', 3)
      .style('opacity', 0.8);

    // Modified value label with power value only
  // Value labels
bars.append('text')
  .attr('class', 'value-label')
  .attr('y', (d: PowerDataItem) => yScale(d.label)! + yScale.bandwidth() / 2)
  .attr('x', (d: PowerDataItem) => {
    if (d.value >= 0) {
      // For positive values (resistance) - position at bar end
      const barEnd = zeroLinePosition + Math.abs(xScale(d.value) - xScale(0));
      return barEnd + 8;
    } else {
      // For negative values (contribution) - position PERCENTAGE first, then value
      const barStart = zeroLinePosition + xScale(d.value) - xScale(0);
      const percentageText = d.percentage && d.percentage !== 0 ? 
        `(${d.percentage >= 0 ? '+' : ''}${d.percentage}%)` : '';
      const percentageWidth = this.getTextWidth(percentageText);
      const minSpacing = 10;
      
      // Position value AFTER percentage space
      return barStart - 8 - percentageWidth - minSpacing;
    }
  })
  .attr('dy', '0.35em')
  .attr('text-anchor', (d: PowerDataItem) => d.value >= 0 ? 'start' : 'end')
  .style('font-size', '12px')
  .style('font-weight', '700')
  .style('fill', '#1e293b')
  .style('text-shadow', '1px 1px 2px rgba(255,255,255,0.9)')
  .text((d: PowerDataItem) => this.powerChartService.formatPowerValue(d.value, this.currentDisplayFormat));

// Percentage labels
bars.append('text')
  .attr('class', 'percentage-label')
  .attr('y', (d: PowerDataItem) => yScale(d.label)! + yScale.bandwidth() / 2)
  .attr('x', (d: PowerDataItem) => {
    if (d.value >= 0) {
      // For positive values (resistance) - position to the right of power value
      const barEnd = zeroLinePosition + Math.abs(xScale(d.value) - xScale(0));
      const powerValueWidth = this.getTextWidth(this.powerChartService.formatPowerValue(d.value || 0, this.currentDisplayFormat));
      const minSpacing = 10;
      return barEnd + 8 + powerValueWidth + minSpacing;
    } else {
      // For negative values (contribution) - position CLOSEST to bar (right side of percentage area)
      const barStart = zeroLinePosition + xScale(d.value) - xScale(0);
      return barStart - 8;
    }
  })
  .attr('dy', '0.35em')
  .attr('text-anchor', (d: PowerDataItem) => d.value >= 0 ? 'start' : 'end')
  .style('font-size', '12px')
  .style('font-weight', '500')
  .style('fill', (d: PowerDataItem) => {
    if (d.percentage && d.percentage !== 0) {
      return d.percentage >= 0 ? '#ff5252' : '#4caf50';
    }
    return '#1e293b';
  })
  .style('text-shadow', '1px 1px 2px rgba(255,255,255,0.9)')
  .text((d: PowerDataItem) => {
    if (d.percentage && d.percentage !== 0) {
      const sign = d.percentage >= 0 ? '+' : '';
      return `(${sign}${d.percentage}%)`;
    }
    return '';
  });

    bars.append('text')
      .attr('class', 'category-label')
      .attr('y', (d: PowerDataItem) => yScale(d.label)! + yScale.bandwidth() / 2)
      .attr('x', -125)
      .attr('dy', '0.35em')
      .attr('text-anchor', 'end')
      .style('font-size', '11px')
      .style('font-weight', '600')
      .style('fill', '#374151')
      .style('text-shadow', '0.5px 0.5px 1px rgba(255,255,255,0.8)')
      .text((d: PowerDataItem) => d.label);

    const totalPower = data.reduce((sum: number, d: PowerDataItem) => sum + d.value, 0);
    
    (svg as any).container.selectAll('.total-power-display').remove();
    
    (svg as any).container
      .append('div')
      .attr('class', 'total-power-display')
      .style('text-align', 'center')
      .style('background', 'linear-gradient(135deg, #dbeafe 0%, #bfdbfe 100%)')
      .style('border', '2px solid #3b82f6')
      .style('border-radius', '6px')
      .style('color', '#1e40af')
      .style('font-size', '13px')
      .style('font-weight', '700')
      .style('box-shadow', '0 2px 4px rgba(59, 130, 246, 0.2)')
      .html(`Total: ${this.powerChartService.formatTotalPowerValue(totalPower, this.currentDisplayFormat)}`);
  }

   private getTextWidth(text: string): number {
    let width = 0;
    for (let char of text) {
      if (char === '(' || char === ')' || char === '%' || char === '+' || char === '-') {
        width += 5; // Smaller characters
      } else if (char === ' ') {
        width += 4; // Space is narrower
      } else {
        width += 7; // Numbers and letters
      }
    }
    return width;
  }
  private getSimpleBarColor(value: number): string {
    return value >= 0 ? '#ef4444' : '#10b981';
  }
}