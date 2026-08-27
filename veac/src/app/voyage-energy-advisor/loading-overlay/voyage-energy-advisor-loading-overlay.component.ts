import { Component } from '@angular/core';
import { VoyageService } from '../../services/state/voyage-scheduler.service';
import { CommonModule } from '@angular/common';
import { ProgressService } from '../../services/realtime/progress.service'; 
import { ProgressComponent } from '../../shared/components/progress/progress.component';

@Component({
  selector: 'app-voyage-energy-advisor-loading-overlay',
  standalone: true,
  templateUrl: './voyage-energy-advisor-loading-overlay.component.html',
  styleUrls: ['./voyage-energy-advisor-loading-overlay.component.css'],
  imports: [CommonModule,ProgressComponent]
})
export class VoyageEnergyAdvisorLoadingOverlayComponent {
  description = '';

  constructor(
    public voyageService: VoyageService,
    private progressService: ProgressService) {
      this.progressService.description$.subscribe(desc => {
        this.description = desc;
      });
  }

  public cancelRequest() {
    this.voyageService.cancelRequest();
  }

  public dismissError(): void {
    this.voyageService.errorMessage = ''; 
  }
}
