import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { ValidationWarning } from '../../../calculations/calculator.types';

@Component({
  selector: 'app-validation-warnings',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatIconModule],
  templateUrl: './validation-warnings.component.html',
  styleUrl: './validation-warnings.component.css'
})
export class ValidationWarningsComponent {
  @Input() warnings: ValidationWarning[] = [];

  trackByWarning(_index: number, warning: ValidationWarning): string {
    return (warning.type || warning.field) + warning.severity;
  }

  getWarningTitle(type: string): string {
    const titles: Record<string, string> = {
      'main-engine': 'Main Engine Overcapacity',
      'aux-engine': 'Auxiliary Engine Overcapacity',
      'hotel-load': 'Hotel Load Exceeds System Capacity',
      'shaft-generator': 'Shaft Generator Capacity Warning',
      'shaft-capacity': 'Configuration Error'
    };
    return titles[type] || 'Warning';
  }
}
