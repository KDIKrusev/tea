import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ThemeService } from '../../services/ui/theme/theme.service';

@Component({
  selector: 'app-palette-selector',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './palette-selector.component.html',
  styleUrls: ['./palette-selector.component.css']
})
export class PaletteSelectorComponent implements OnInit {
    public selectedTheme!: string;
    constructor(public themeService: ThemeService) {
    }
    public isDropdownOpen = false; 

    ngOnInit(): void {
        const theme = Promise.resolve(this.themeService.getSelectedTheme());
        theme.then((result) => {
            this.selectedTheme = result;
        });
    }

    public changeTheme(theme: string) {
        this.themeService.themeChanged.emit(theme);
        this.themeService.updateSelectedTheme(theme);
        this.selectedTheme = theme;
    }

    public toggleDropdown(event: Event): void {
        this.isDropdownOpen = !this.isDropdownOpen;
        event.stopPropagation(); // Prevent event propagation to the document
      }
}
