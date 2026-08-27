import { Component, Input, Output, EventEmitter, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-select-input',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './select-input.component.html',
  styleUrls: ['./select-input.component.css']
})
export class SelectInputComponent {
  @Input() options: string[] = [];
  @Input() selected: string = '';
  @Input() label?: string;
  @Output() selectedChange = new EventEmitter<string>();

  ngOnChanges(changes: SimpleChanges) {
    if (changes['selected'] && !changes['selected'].firstChange && changes['selected'].currentValue) {
      this.onChange();
    }
  }

  onChange(): void {
    this.selectedChange.emit(this.selected);
  }
}