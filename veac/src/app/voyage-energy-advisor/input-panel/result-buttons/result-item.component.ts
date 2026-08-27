import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface ResultOption {
  id: string;
  icon: string;
  label: string;
  details?: any;
}

@Component({
  selector: 'app-result-item',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './result-item.component.html',
  styleUrls: ['./result-item.component.css']
})
export class ResultItemComponent {
  @Input() result!: ResultOption;
  @Output() selected = new EventEmitter<ResultOption>();
  
  onSelect(): void {
    this.selected.emit(this.result);
  }
}