import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { CalculatorInput, AllVariantsCalculationResult } from './calculator.types';
import { ConfigService } from '../core/config.service';

@Injectable({
  providedIn: 'root'
})
export class CalculatorService {
  private http = inject(HttpClient);
  private configService = inject(ConfigService);

  calculateAllVariants(input: CalculatorInput): Observable<AllVariantsCalculationResult> {
    const apiUrl = this.configService.apiUrl;
    return this.http.post<AllVariantsCalculationResult>(`${apiUrl}/api/calculator/calculate-all-variants`, input);
  }
}
