import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { CalculatorPageComponent } from './features/calculator-page/calculator-page.component';
import { AppDataService } from './core/app-data.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CalculatorPageComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  title = 'k-sail-calculator';
  private appDataService = inject(AppDataService);

  ngOnInit(): void {
    // Load ALL static application data in a single optimized API call
    // This replaces multiple separate calls to vessel types, engines, and operational profiles
    this.appDataService.loadInitialData().subscribe({
      next: () => {},
      error: () => {}
    });
  }
}
