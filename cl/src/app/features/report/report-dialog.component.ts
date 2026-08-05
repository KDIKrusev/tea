import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ReportData, ReportService } from './report.service';

/**
 * Pre-flight dialog for the client report: asks for an optional client / project
 * name and opens the report tab DIRECTLY from the button click — window.open must
 * stay inside the user gesture, or popup blockers kill it (afterClosed fires after
 * the dialog close animation, outside the gesture).
 */
@Component({
  selector: 'app-report-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  template: `
    <h2 mat-dialog-title>Generate client report</h2>
    <mat-dialog-content>
      <p class="dlg-hint">
        Creates a one-page "Energy Savings Assessment" in a new tab,
        ready to print or save as PDF and share with the client.
      </p>
      <mat-form-field appearance="fill" class="dlg-field">
        <mat-label>Client / project name (optional)</mat-label>
        <input matInput [(ngModel)]="clientName" maxlength="80"
               placeholder="e.g. Aegean Bulk Shipping Ltd."
               (keyup.enter)="generate()">
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" (click)="generate()">
        <mat-icon>description</mat-icon>
        Generate report
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    .dlg-hint { font-size: 13px; color: rgba(0,0,0,.6); margin-bottom: 14px; max-width: 360px; }
    .dlg-field { width: 100%; }
  `]
})
export class ReportDialogComponent {
  private dialogRef = inject<MatDialogRef<ReportDialogComponent>>(MatDialogRef);
  private reportService = inject(ReportService);
  private snackBar = inject(MatSnackBar);
  private data = inject<ReportData>(MAT_DIALOG_DATA);

  clientName = '';

  generate(): void {
    const trimmed = this.clientName.trim();
    const opened = this.reportService.open({
      ...this.data,
      clientName: trimmed.length > 0 ? trimmed : null
    });
    if (!opened) {
      this.snackBar.open(
        'The report tab was blocked by the browser. Please allow pop-ups for this site and try again.',
        'Close',
        { duration: 6000, panelClass: ['error-snackbar'] }
      );
      return;
    }
    this.dialogRef.close();
  }
}
