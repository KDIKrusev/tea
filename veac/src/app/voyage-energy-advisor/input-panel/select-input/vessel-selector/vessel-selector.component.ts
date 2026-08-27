import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SelectInputComponent } from '../select-input.component';
import { VoyageService } from '../../../../services/state/voyage-scheduler.service';
import { FormsModule } from '@angular/forms';
import { Vessel } from '../../../../models/entities/vessel.model';
import { AuthService } from '../../../../services/auth/auth.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-vessel-selector',
  standalone: true,
  imports: [CommonModule, FormsModule, SelectInputComponent],
  templateUrl: './vessel-selector.component.html',
  styleUrls: ['./vessel-selector.component.css']
})
export class VesselSelectorComponent implements OnInit {
  @Output() vesselSelected = new EventEmitter<{name: string, id: number}>();

  vessels: string[] = [];
  selectedVessel: string = '';
  vesselList: Vessel[] = [];
  selectedVesselId: number | null = null;
  loading: boolean = false;
  error: string | null = null;
  private loginSubscription!: Subscription;

  constructor(private voyageService: VoyageService,  private authService: AuthService) {}

  async ngOnInit() {
    await this.loadVessels();

     this.loginSubscription = this.authService.loginNotifier$.subscribe(() => {
      this.loadVessels();
    });

  }

  ngOnDestroy() {
    if (this.loginSubscription) {
      this.loginSubscription.unsubscribe();
    }
  }

 async loadVessels() {
  try {
    this.loading = true;
    this.error = null;

    this.vesselList = await this.voyageService.loadVessels();
    this.vessels = this.vesselList.map(vessel => vessel.name || `Vessel ${vessel.id}`);

    if (this.vessels.length > 0) {
      this.selectedVessel = this.vessels[0];
      this.selectedVesselId = this.vesselList[0].id;
      // Set selected vessel locally
      this.voyageService.setSelectedVessel(this.selectedVesselId);

      this.vesselSelected.emit({
        name: this.selectedVessel,
        id: this.selectedVesselId
      });
    }
  } catch (error) {
      this.voyageService.showGenericLoadError();
  } finally {
    this.loading = false;
  }
}

  async onSelectedVesselChanged(vessel: string): Promise<void> {
    const selectedVesselObj = this.vesselList.find(v => v.name === vessel);

    if (!selectedVesselObj) {
      console.warn("⚠️ No vessel found for selection:", vessel);
      return;
    }

    this.selectedVesselId = selectedVesselObj.id;

    if (this.selectedVesselId !== null) {
      try {
        await this.authService.refreshTokenForVessel(this.selectedVesselId);

        this.voyageService.setSelectedVessel(this.selectedVesselId);

        this.vesselSelected.emit({
          name: vessel,
          id: this.selectedVesselId
        });
      } catch (error) {
        console.error("❌ Error updating vessel:", error);
        this.error = 'Failed to update vessel selection';
      }
    } else {
      console.warn("⚠️ No vessel selected, skipping update.");
    }
  }
}