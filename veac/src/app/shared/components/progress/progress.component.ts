import { Component, OnInit  } from '@angular/core';
import { ProgressService } from '../../../services/realtime/progress.service';

@Component({
  standalone: true, 
  selector: 'app-progress',
  templateUrl: './progress.component.html',
  styleUrls: ['./progress.component.css']
})
export class ProgressComponent implements OnInit {
  progress = 0;

  constructor(private progressService: ProgressService) {}

  ngOnInit() {

    this.progressService.resetProgress(); 

    this.progressService.progress$.subscribe(value => {
      this.progress = parseFloat(value.toFixed(2)); 
    });
  }

}
