import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatExpansionModule } from '@angular/material/expansion';
import { PowerDemands, ModePowerBreakdown } from '../../../calculations/calculator.types';

@Component({
  selector: 'app-power-demands-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, MatCardModule, MatIconModule, MatExpansionModule],
  templateUrl: './power-demands-panel.component.html',
  styleUrl: './power-demands-panel.component.css'
})
export class PowerDemandsPanelComponent {
  @Input() powerDemands!: PowerDemands;

  get hasSail(): boolean {
    return this.powerDemands?.modeBreakdowns?.some(m => m.propulsionSailKw > 0) ?? false;
  }

  trackByMode(_index: number, mode: ModePowerBreakdown): string {
    return mode.mode;
  }
}
