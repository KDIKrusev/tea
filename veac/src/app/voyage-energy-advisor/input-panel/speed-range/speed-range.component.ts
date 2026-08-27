// In speed-range.component.ts
import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Options, NgxSliderModule } from '@angular-slider/ngx-slider';

@Component({
  selector: 'app-speed-range',
  standalone: true,
  imports: [CommonModule, FormsModule, NgxSliderModule],
  templateUrl: './speed-range.component.html',
  styleUrls: ['./speed-range.component.css']
})
export class SpeedRangeComponent {
  @Input() heading: string = 'Speed Window';
  @Input() speedMin: number = 13; // Default changed to 13
  @Input() speedMax: number = 17; // Default changed to 17
  @Input() options: Options = {
    floor: 5,    // Changed to 5
    ceil: 25,    // Changed to 25
    step: 1,
    showTicks: false,
    draggableRange: true
  };
  
  @Output() speedChanged = new EventEmitter<{min: number, max: number}>();
  
  onSpeedChange(): void {
    this.speedChanged.emit({
      min: this.speedMin,
      max: this.speedMax
    });
  }
}