import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-ea-panel',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ea-panel.component.html',
  styleUrls: ['./ea-panel.component.css']
})
export class EaPanelComponent {
  @Input() heading!: string;
  @Input() subHeading?: string;
  @Input() hideHeader = false;
  @Input() hideHeaderSeparator = false;
}
