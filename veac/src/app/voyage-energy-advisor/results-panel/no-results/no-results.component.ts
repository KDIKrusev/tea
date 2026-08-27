import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { VoyageOriginalRequest } from '../../../models/api/voyage-original-request.model';

@Component({
  selector: 'app-no-results',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './no-results.component.html',
  styleUrls: ['./no-results.component.css']
})
export class NoResultsComponent {
  @Input() originalRequestData!: VoyageOriginalRequest;
}