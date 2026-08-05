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
    // Warm the single static-data payload (engine catalogue, categories, fuel prices) so it is
    // already cached by the time the form's sections ask for it.
    //
    // Both outcomes are deliberately ignored here: the components that need the data subscribe to
    // it themselves and each shows its own error message, so handling it a second time in the root
    // component would only produce a duplicate snackbar.
    this.appDataService.loadInitialData().subscribe({
      error: () => undefined
    });
  }
}
