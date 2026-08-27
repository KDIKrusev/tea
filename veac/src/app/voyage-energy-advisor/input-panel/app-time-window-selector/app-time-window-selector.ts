import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-time-window-selector',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app-time-window-selector.html',
  styleUrls: ['./app-time-window-selector.css']
})
export class TimeWindowSelectorComponent {
  @Input() heading: string = 'Time Window';
  @Input() selectedMode: 'etd' | 'eta' = 'etd';
  @Output() modeChanged = new EventEmitter<'etd' | 'eta'>();
  
  selectMode(mode: 'etd' | 'eta'): void {
    this.selectedMode = mode;
    this.modeChanged.emit(mode);
  }
}