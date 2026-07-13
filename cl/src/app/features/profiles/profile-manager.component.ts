import {
  Component, Output, EventEmitter, OnInit,
  ChangeDetectionStrategy, ChangeDetectorRef, ViewChild, ElementRef
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCardModule } from '@angular/material/card';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ProfileService } from '../../core/profile.service';
import { SavedProfile } from '../../core/profile.types';
import { CalculatorInput } from '../../calculations/calculator.types';

/** Emitted when the user clicks Save and has typed a name — parent provides the actual input data */
export interface SaveRequestEvent {
  name: string;
}

@Component({
  selector: 'app-profile-manager',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatIconModule,
    MatInputModule,
    MatFormFieldModule,
    MatCardModule,
    MatTooltipModule,
    MatExpansionModule,
    MatSnackBarModule
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile-manager.component.html',
  styleUrl: './profile-manager.component.css'
})
export class ProfileManagerComponent implements OnInit {

  @Output() profileLoadRequested = new EventEmitter<SavedProfile>();
  /** Parent listens for this, then calls confirmSaveWithData() */
  @Output() saveRequest = new EventEmitter<SaveRequestEvent>();
  @ViewChild('importInput') importInput!: ElementRef<HTMLInputElement>;

  profiles: SavedProfile[] = [];
  saveDialogOpen = false;
  newProfileName = '';
  renamingId: string | null = null;
  renameValue = '';
  deleteConfirmId: string | null = null;

  constructor(
    private profileService: ProfileService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.profiles = this.profileService.getAll().sort(
      (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
    );
    this.cdr.markForCheck();
  }

  // ─── SAVE ────────────────────────────────────────────────────────────────────

  openSaveDialog(): void {
    this.saveDialogOpen = true;
    this.newProfileName = '';
    this.cdr.markForCheck();
  }

  cancelSave(): void {
    this.saveDialogOpen = false;
    this.cdr.markForCheck();
  }

  /** Called from template when user confirms; parent supplies actual form data */
  saveRequested(): void {
    if (!this.newProfileName.trim()) return;
    this.saveRequest.emit({ name: this.newProfileName.trim() });
  }

  /** Called by parent component after it receives saveRequest, passing the current form data */
  confirmSaveWithData(
    input: CalculatorInput,
    vesselTypeName: string,
    vesselCategory: string,
    vesselSize: number,
    vesselSpeed: number
  ): void {
    if (!this.newProfileName.trim()) return;
    try {
      this.profileService.save(this.newProfileName, input, vesselTypeName, vesselCategory, vesselSize, vesselSpeed);
      this.saveDialogOpen = false;
      this.newProfileName = '';
      this.refresh();
      this.snackBar.open('Profile saved', 'OK', { duration: 2500 });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Unable to save profile.';
      this.snackBar.open(msg, 'Close', { duration: 5000, panelClass: ['error-snackbar'] });
    }
  }

  // ─── LOAD ────────────────────────────────────────────────────────────────────

  loadProfile(profile: SavedProfile): void {
    this.profileLoadRequested.emit(profile);
    this.snackBar.open(`Loaded: ${profile.name}`, 'OK', { duration: 2500 });
  }

  // ─── RENAME ──────────────────────────────────────────────────────────────────

  startRename(profile: SavedProfile): void {
    this.renamingId = profile.id;
    this.renameValue = profile.name;
    this.cdr.markForCheck();
  }

  confirmRename(): void {
    if (!this.renamingId || !this.renameValue.trim()) {
      this.renamingId = null;
      this.cdr.markForCheck();
      return;
    }
    try {
      this.profileService.rename(this.renamingId!, this.renameValue);
      this.renamingId = null;
      this.refresh();
      this.snackBar.open('Profile renamed', 'OK', { duration: 2000 });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Unable to rename profile.';
      this.snackBar.open(msg, 'Close', { duration: 5000, panelClass: ['error-snackbar'] });
    }
  }

  cancelRename(): void {
    this.renamingId = null;
    this.cdr.markForCheck();
  }

  // ─── DELETE ──────────────────────────────────────────────────────────────────

  requestDelete(id: string): void {
    this.deleteConfirmId = id;
    this.cdr.markForCheck();
  }

  confirmDelete(): void {
    if (!this.deleteConfirmId) return;
    try {
      this.profileService.delete(this.deleteConfirmId!);
      this.deleteConfirmId = null;
      this.refresh();
      this.snackBar.open('Profile deleted', 'OK', { duration: 2000 });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Unable to delete profile.';
      this.snackBar.open(msg, 'Close', { duration: 5000, panelClass: ['error-snackbar'] });
    }
  }

  cancelDelete(): void {
    this.deleteConfirmId = null;
    this.cdr.markForCheck();
  }

  // ─── EXPORT ──────────────────────────────────────────────────────────────────

  exportProfile(profile: SavedProfile, event: MouseEvent): void {
    event.stopPropagation();
    this.profileService.exportToJson(profile);
    this.snackBar.open('Profile exported', 'OK', { duration: 2000 });
  }

  // ─── IMPORT ──────────────────────────────────────────────────────────────────

  triggerImport(): void {
    this.importInput.nativeElement.value = '';
    this.importInput.nativeElement.click();
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    try {
      await this.profileService.importFromJson(file);
      this.refresh();
      this.snackBar.open('Profile imported successfully', 'OK', { duration: 3000 });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Import failed';
      this.snackBar.open(msg, 'Close', { duration: 5000, panelClass: ['error-snackbar'] });
    }
  }

  // ─── HELPERS ─────────────────────────────────────────────────────────────────

  formatDate(iso: string): string {
    try {
      return new Date(iso).toLocaleDateString(undefined, {
        year: 'numeric', month: 'short', day: 'numeric',
        hour: '2-digit', minute: '2-digit'
      });
    } catch {
      return iso;
    }
  }

  trackById(_: number, profile: SavedProfile): string {
    return profile.id;
  }
}
