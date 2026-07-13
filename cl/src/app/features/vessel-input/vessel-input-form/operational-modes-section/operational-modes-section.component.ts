import { Component, Input, OnInit, OnDestroy, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatCheckboxModule, MatCheckboxChange } from '@angular/material/checkbox';
import { FormInputFieldComponent } from '../../../../shared/components';
import { AppDataService } from '../../../../core/app-data.service';
import { VesselOperationalProfile } from '../../../../core/operational-profile.types';

@Component({
  selector: 'app-operational-modes-section',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    MatTooltipModule,
    MatExpansionModule,
    MatCheckboxModule,
    FormInputFieldComponent
  ],
  templateUrl: './operational-modes-section.component.html',
  styleUrl: './operational-modes-section.component.css'
})
export class OperationalModesSectionComponent implements OnInit, OnDestroy {
  @Input() parentForm!: FormGroup;
  @Input() vesselTypeName: string | null = null;
  @Output() operationalProfileLoaded = new EventEmitter<VesselOperationalProfile>();

  private destroy$ = new Subject<void>();
  private appDataService = inject(AppDataService);

  operationalProfile: VesselOperationalProfile | null = null;
  isLoading = false;
  componentsLoaded = false;
  dpModeEnabled = false; // Controls DP mode visibility

  ngOnInit(): void {
    this.componentsLoaded = true;
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Set operational profile directly (from optimized combined API call)
   * OPTIMIZED: No separate API call needed
   */
  setOperationalProfile(profile: VesselOperationalProfile): void {
    this.operationalProfile = profile;
    this.dpModeEnabled = profile.dP !== null && profile.dP !== undefined; // Enable if vessel has DP
    this.populateFormWithProfile(profile);
    this.operationalProfileLoaded.emit(profile);
  }

  /**
   * Populate form controls with operational profile data
   * REFACTORED V2.0: Uses profile hours directly (already in h/yr from database)
   */
  private populateFormWithProfile(profile: VesselOperationalProfile): void {
    if (!this.parentForm) return;

    // Use profile hours directly - no scaling needed
    this.parentForm.patchValue({
      // Transit Mode
      transitHours: profile.transit.annualHours,
      transitHotelPowerKW: profile.transit.hotelLoadPowerKW,

      // DP Mode (if exists)
      dpHours: profile.dP?.annualHours || 0,
      dpHotelPowerKW: profile.dP?.hotelLoadPowerKW || 0,
      requiredDPPowerKW: profile.dP?.requiredDPPowerKW || 0,
      dpWeatherCondition: 'Moderate',

      // Port Mode
      portHotelPowerKW: profile.port?.hotelLoadPowerKW || 0,
      portHours: profile.port?.annualHours || 0,

      // Anchor Mode
      anchorHotelPowerKW: profile.anchor?.hotelLoadPowerKW || 0,
      anchorHours: profile.anchor?.annualHours || 0,

      // Maneuvering Mode
      maneuveringPropulsionPowerKW: profile.maneuvering?.propulsionPowerKW || 0,
      maneuveringHotelPowerKW: profile.maneuvering?.hotelLoadPowerKW || 0,
      maneuveringHours: profile.maneuvering?.annualHours || 0
    });
  }

  /**
   * Calculate percentage of mode hours relative to total actual hours
   * Shows what percentage of total operational time is spent in this mode
   */
  getPercentage(hours: number): number {
    const numericHours = Number(hours) || 0;
    const totalHours = this.getTotalHours();
    
    if (totalHours === 0) return 0;
    
    return (numericHours / totalHours) * 100;
  }

  /**
   * Get total hours from form
   */
  getTotalHours(): number {
    if (!this.parentForm) return 0;

    const transitHours = Number(this.parentForm.get('transitHours')?.value) || 0;
    const dpHours = Number(this.parentForm.get('dpHours')?.value) || 0;
    const portHours = Number(this.parentForm.get('portHours')?.value) || 0;
    const anchorHours = Number(this.parentForm.get('anchorHours')?.value) || 0;
    const maneuveringHours = Number(this.parentForm.get('maneuveringHours')?.value) || 0;

    return transitHours + dpHours + portHours + anchorHours + maneuveringHours;
  }

  /**
   * Has DP mode capabilities (vessel supports DP)
   */
  hasDPMode(): boolean {
    return this.operationalProfile?.dP !== null && this.operationalProfile?.dP !== undefined;
  }

  /**
   * Handle DP Mode checkbox toggle
   */
  onDPModeToggle(event: MatCheckboxChange): void {
    if (!this.dpModeEnabled) {
      // Reset DP fields when disabled
      this.parentForm.patchValue({
        dpHours: 0,
        dpHotelPowerKW: 0,
        requiredDPPowerKW: 0
      });
    } else if (this.operationalProfile?.dP) {
      // Restore original values when enabled
      this.parentForm.patchValue({
        dpHours: this.operationalProfile.dP.annualHours,
        dpHotelPowerKW: this.operationalProfile.dP.hotelLoadPowerKW,
        requiredDPPowerKW: this.operationalProfile.dP.requiredDPPowerKW || 0
      });
    }
  }
}
