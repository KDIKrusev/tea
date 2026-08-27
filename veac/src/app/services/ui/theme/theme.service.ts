import { Injectable, Output, EventEmitter } from '@angular/core';
import { SettingTypeName } from './theme.constants';
import { ThemeList } from './theme.enum';

@Injectable({
  providedIn: 'root'  
})
export class ThemeService {
  @Output() themeChanged = new EventEmitter<string>();
  public selectedTheme: string = ThemeList[ThemeList.Day];
  private defaultThemeSetting: string;
  private themeSettingKey: string;
  private isInitialized = false;

  constructor() {
    this.defaultThemeSetting = ThemeList.Day;
    this.themeSettingKey = SettingTypeName;
  }

  /**
   * To apply the selected theme
   */
  async applySelectedTheme(): Promise<void> {
    this.selectedTheme = this.getSelectedTheme();
    this.changeThemeFiles(this.selectedTheme);
  }

  /**
   * To get the selected theme after app is loaded
   */
  public getSelectedTheme(): string {
    const result = null; // this.localStorageService.get<string>(this.themeSettingKey);
    if (!result) {
      this.updateSelectedTheme(this.defaultThemeSetting);
      return this.defaultThemeSetting;
    } else {
      return result;
    }
  }

  /**
   * To update selected theme to API
   * @param theme Selected Theme Name
   * @example theme: 'Day','Dusk' or 'Night'
   */
  async updateSelectedTheme(theme: string) {
    if (!Object.values(ThemeList).includes(ThemeList[theme as keyof typeof ThemeList])) {
      throw new Error('Please select valid theme - Day, Dusk or Night.');
    }

    this.changeThemeFiles(theme);
    this.selectedTheme = theme;
 
  }

  /**
   * To toggle Design System and application-specific theme CSS files
   */
  private async changeThemeFiles(theme: string): Promise<void> {
    // Load in parallel:
    // - Design System CSS files follow the pattern "kx-day-theme.css", "kx-dusk-theme.css", etc.
    // - App-specific CSS files follow the pattern "app-day-theme.css", "app-dusk-theme.css", etc.

    await Promise.all([this.changeThemeFile('kx', theme), this.changeThemeFile('app', theme)]);
  }

  private async changeThemeFile(prefix: string, theme: string): Promise<void> {
    return new Promise((resolve, reject) => {
        try {

            const newThemeFile = `/styles/${prefix}-${theme.toLowerCase()}-theme.css`;
            const styleSelectors = Object.keys(ThemeList)
                .map((themeName: string) => `link[rel=stylesheet][href='/styles${prefix}-${themeName.toLowerCase()}-theme.css']`)
                .join(',');

            let styleElement: HTMLLinkElement | null = document.querySelector(styleSelectors);

            // New addition: remove any existing theme-related styles before adding the new one
            const existingThemeLinks = document.querySelectorAll(`link[rel=stylesheet][href*='${prefix}-']`);
            existingThemeLinks.forEach(link => {
                document.head.removeChild(link);
            });

            if (!styleElement) {
                // First time trying to load lazy theme, in this case the style element must be created
                styleElement = document.createElement('link');
                styleElement.rel = 'stylesheet';
                document.head.appendChild(styleElement);
                styleElement.href = newThemeFile;
                if (!styleElement.onload) {
                    styleElement.onload = () => {
                        resolve();
                    };
                }
            } else {
                // Executed only on changing theme,
                // New link style element must be created with new theme file and removing old one when new theme file is loaded
                const newStyleElement = document.createElement('link');
                newStyleElement.rel = 'stylesheet';
                newStyleElement.href = newThemeFile;

                document.head.insertBefore(newStyleElement, styleElement);
                // To remove old theming file
                newStyleElement.onload = () => {
                    if (styleElement) {
                        document.head.removeChild(styleElement);
                    }
                    resolve();
                };
            }
        } catch (error) {
            reject(error);
        }
    });
}

  /**
   * To check setting type exists
   * @param settingTypeName Setting Type Name
   * @param settingType SettingType Model
   */
  
}
