import { Component, Inject, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
	selector: 'app-vessel-config-confirm-dialog',
	standalone: true,
	imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
	template: `
		<div class="dialog-container">
			<div class="dialog-header">
				<mat-icon class="header-icon">sync_alt</mat-icon>
				<h2>Vessel Type Changed</h2>
			</div>
			
			<div class="dialog-content">
				<p>You are switching to <strong>{{ data.vesselTypeName }}</strong>.</p>
				<p>Your edited values can be kept, or you can start fresh with defaults for this vessel type.</p>
				<p class="info-text">
					<mat-icon class="info-icon">info</mat-icon>
					Keep my values = preserve your current form values. Reset = full clean defaults.
				</p>
			</div>
			
			<div class="dialog-actions">
				<button mat-stroked-button (click)="onKeepValues()" class="keep-btn">
					<mat-icon>check_circle_outline</mat-icon>
					Keep my values
				</button>
				<button mat-raised-button (click)="onResetDefaults()" class="reset-btn">
					<mat-icon>restart_alt</mat-icon>
					Reset to new defaults
				</button>
			</div>
		</div>
	`,
	styles: [`
		.dialog-container {
			min-width: 360px;
			padding: 24px;
			font-family: Roboto, sans-serif;
		}

		.dialog-header {
			display: flex;
			align-items: center;
			gap: 12px;
			margin-bottom: 16px;
			border-bottom: 2px solid #90caf9;
			padding-bottom: 12px;
		}

		.header-icon {
			font-size: 24px;
			width: 24px;
			height: 24px;
			color: #1565c0;
		}

		.dialog-header h2 {
			margin: 0;
			font-size: 18px;
			font-weight: 600;
			color: #212121;
		}

		.dialog-content {
			margin: 16px 0;
			line-height: 1.5;
			color: #424242;
		}

		.dialog-content p {
			margin: 10px 0;
		}

		.info-text {
			display: flex;
			align-items: flex-start;
			gap: 8px;
			background: #f5f9ff;
			color: #1f2937;
			padding: 12px;
			border-radius: 8px;
			margin-top: 14px;
			border: 1px solid #dbeafe;
		}

		.info-icon {
			font-size: 18px;
			width: 18px;
			height: 18px;
			margin-top: 2px;
			flex-shrink: 0;
			color: #1565c0;
		}

		.dialog-actions {
			display: flex;
			justify-content: flex-end;
			gap: 12px;
			margin-top: 20px;
			padding-top: 16px;
			border-top: 1px solid #e0e0e0;
			flex-wrap: wrap;
		}

		.keep-btn, .reset-btn {
			display: flex;
			align-items: center;
			gap: 6px;
			font-weight: 600;
		}

		.keep-btn {
			color: #1565c0;
			border-color: #90caf9 !important;
		}

		.keep-btn:hover {
			background-color: #eff6ff;
		}

		.reset-btn {
			background-color: #1565c0 !important;
			color: #ffffff !important;
		}

		.reset-btn:hover {
			background-color: #0d47a1 !important;
		}

		.reset-btn mat-icon,
		.reset-btn .mdc-button__label {
			color: #ffffff !important;
		}

		mat-icon {
			user-select: none;
		}

		@media (max-width: 480px) {
			.dialog-container {
				min-width: 0;
				width: 100%;
				padding: 16px;
			}

			.dialog-actions {
				justify-content: stretch;
			}

			.keep-btn,
			.reset-btn {
				width: 100%;
				justify-content: center;
			}
		}
	`],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class VesselConfigConfirmDialogComponent {
	constructor(
		public dialogRef: MatDialogRef<VesselConfigConfirmDialogComponent>,
		@Inject(MAT_DIALOG_DATA) public data: { vesselTypeName: string }
	) {}

	onKeepValues(): void {
		this.dialogRef.close('keep');
	}

	onResetDefaults(): void {
		this.dialogRef.close('reset');
	}
}
