import { Component, OnInit, OnDestroy, ViewChild, HostListener } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { UtcTimeService } from '../services/utilities/utc-time.service';
import { ThemeService } from '../services/ui/theme/theme.service';
import { TimeSyncService } from '../services/utilities/time-sync.service';
import { PaletteSelectorComponent } from '../top-bar/palette-selector/palette-selector.component';
import { SettingsDialogComponent } from './settings-dialog/settings-dialog.component';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth/auth.service';
import { VoyageService } from '../services/state/voyage-scheduler.service';

@Component({
  selector: 'app-top-bar',
  standalone: true,
  imports: [CommonModule, PaletteSelectorComponent, RouterModule, SettingsDialogComponent],
  templateUrl: './top-bar.component.html',
  styleUrls: ['./top-bar.component.css']
})
export class TopBarComponent implements OnInit, OnDestroy {
  @ViewChild('settingsDialog') settingsDialog!: SettingsDialogComponent;
  
  public date!: string;
  public time!: string;
  public buildVersions = 'Failed to fetch file!';
  public fileName = 'buildVersions.txt';
  public utcDate!: string;
  public utcTime!: string;
  public showVersionButton = true;
  public isMenuOpen = false;

  constructor(
    public themeService: ThemeService,
    private utcTimeService: UtcTimeService,
    private timeSyncService: TimeSyncService,
    public voyageService: VoyageService,
    private authService: AuthService
  ) {}

  private timeInterval!: NodeJS.Timeout;

  ngOnInit(): void {
    this.timeInterval = setInterval(() => {
      this.setDateAndTime();
    }, 50);
  }

  ngOnDestroy(): void {
    clearInterval(this.timeInterval);
  }

  @HostListener('document:click', ['$event'])
  public onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const hamburgerMenu = target.closest('.vea-hamburger-menu');
    
    if (!hamburgerMenu && this.isMenuOpen) {
      this.isMenuOpen = false;
    }
  }

  public toggleMenu(): void {
    this.isMenuOpen = !this.isMenuOpen;
  }

  public openSettings(): void {
    this.isMenuOpen = false;
    this.settingsDialog.open();
  }

  public logout(): void {
    this.isMenuOpen = false;
    this.authService.logout();
  }

  private setDateAndTime() {
    this.timeSyncService.refreshSystemTime();
    const utcDate = this.utcTimeService.GetUtcDate(this.timeSyncService.systemTimeMicroS);
    this.utcDate = this.formatDate(utcDate);
    this.utcTime = this.formatTime(utcDate);
  }

  public formatDate(utcDate: Date): string {
    const dateOptions: Intl.DateTimeFormatOptions = { day: '2-digit', month: '2-digit', year: 'numeric' };
    return utcDate.toLocaleDateString('en-GB', dateOptions);
  }

  public formatTime(utcTime: Date): string {
    const timeOptions: Intl.DateTimeFormatOptions = { hour: '2-digit', minute: '2-digit', second: '2-digit' };
    return utcTime.toLocaleTimeString('en-GB', timeOptions);
  }
}