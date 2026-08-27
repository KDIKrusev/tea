import { Routes } from '@angular/router';
import { VoyageEnergyAdvisorComponent } from './voyage-energy-advisor/voyage-energy-advisor.component';
import { AuthGuard } from './services/auth/auth.guard'
import { LoginComponent } from './login/login.component'; 
import {LiveModeDashboardComponent} from './live-mode/live-mode-dashboard.component'

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'vec', component: VoyageEnergyAdvisorComponent, canActivate: [AuthGuard] },
  { path: 'live', component: LiveModeDashboardComponent, canActivate: [AuthGuard] },
  { path: '', redirectTo: '/vec', pathMatch: 'full' }
];

