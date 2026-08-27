import { Component, OnInit } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { TopBarComponent } from './top-bar/top-bar.component';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, TopBarComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {

  public initializing!: boolean;

  title = 'VEC.Client';

  constructor(private authService: AuthService, private router: Router) {
    this.initializing = true;
  }

  ngOnInit() {
    if (!this.authService.isAuthenticated()) {
      this.router.navigate(['/login']); 
    }
    this.initializing = false;
  }
}
