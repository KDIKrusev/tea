import { Component, Input, OnInit, OnDestroy, inject, Output, EventEmitter, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { EMPTY, Subject, merge } from 'rxjs';
import { catchError, debounceTime, map, filter, switchMap, takeUntil } from 'rxjs/operators';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormInputFieldComponent, FormSelectFieldComponent } from '../../../../shared/components';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { VesselConfigConfirmDialogComponent } from './vessel-config-confirm-dialog.component';
import { AppDataService } from '../../../../core/app-data.service';
import { FullVesselData, VesselCategoryData } from '../../../../core/app-data.types';
import { VesselTypeWithEnginesResponse } from '../../../../core/vessel-configuration.types';
import { VesselOperationalProfile } from '../../../../core/operational-profile.types';
import { FormEditTrackerService } from '../form-edit-tracker.service';

/** What to do with a fetched vessel config (mirrors the old vesselTypeJustChanged semantics) */
interface FetchRequest {
	applyEngineDefaults: boolean;
	patchPowerFields: boolean;
}

@Component({
	selector: 'app-vessel-config-section',
	standalone: true,
	imports: [
		CommonModule,
		ReactiveFormsModule,
		MatIconModule,
		MatButtonModule,
		MatFormFieldModule,
		MatSelectModule,
		MatInputModule,
		MatExpansionModule,
		FormInputFieldComponent,
		FormSelectFieldComponent,
		MatDialogModule,
		VesselConfigConfirmDialogComponent
	],
	changeDetection: ChangeDetectionStrategy.OnPush,
	templateUrl: './vessel-config-section.component.html',
	styleUrl: './vessel-config-section.component.css'
})
export class VesselConfigSectionComponent implements OnInit, OnDestroy {
	@Input() parentForm!: FormGroup;
	@Output() vesselEngineConfigSelected = new EventEmitter<VesselTypeWithEnginesResponse & { applyEngineDefaults?: boolean }>();
	@Output() operationalProfileLoaded = new EventEmitter<VesselOperationalProfile>();

	private destroy$ = new Subject<void>();
	private appDataService = inject(AppDataService);
	protected editTracker = inject(FormEditTrackerService);
	private snackBar = inject(MatSnackBar);
	private cdr = inject(ChangeDetectorRef);
	private dialog = inject(MatDialog);

	categories: VesselCategoryData[] = [];
	selectedCategory: VesselCategoryData | null = null;

	/** True when the backend clamped the size to the nearest reference curve */
	clampedToReferenceRange = false;

	private fetchTrigger$ = new Subject<FetchRequest>();
	private lastProfileSource: string | null = null;

	private static readonly FETCH_DEBOUNCE_MS = 400;

	get selectedCategoryName(): string | null {
		return this.selectedCategory?.name ?? null;
	}

	get selectedSize(): number | null {
		const value = this.parentForm.get('vesselSize')?.value;
		return value != null && value !== '' ? Number(value) : null;
	}

	get selectedSpeed(): number | null {
		const value = this.parentForm.get('vesselSpeedKnots')?.value;
		return value != null && value !== '' ? Number(value) : null;
	}

	/** Composed selection label, e.g. "Bulk Carrier 75,000 dwt" */
	get selectionLabel(): string {
		if (!this.selectedCategory || this.selectedSize == null) {
			return '';
		}
		return `${this.selectedCategory.name} ${this.selectedSize.toLocaleString('en-US')} ${this.selectedCategory.unit}`;
	}

	get sizeLabel(): string {
		const unit = this.selectedCategory?.unit;
		return unit ? `Vessel Size (${unit.toUpperCase() === 'TEU' ? 'TEU' : unit.toUpperCase()})` : 'Vessel Size';
	}

	get sizeHint(): string {
		const cat = this.selectedCategory;
		if (!cat) {
			return 'Vessel size';
		}
		const min = cat.sizeMin ?? 0;
		if (cat.sizeMax != null) {
			return `${min.toLocaleString('en-US')} – ${cat.sizeMax.toLocaleString('en-US')} ${cat.unit} reference range`;
		}
		return `min ${min.toLocaleString('en-US')} ${cat.unit}`;
	}

	get speedHint(): string {
		const cat = this.selectedCategory;
		return cat ? `${cat.speedMin} – ${cat.speedMax} knots` : 'Vessel speed';
	}

	ngOnInit(): void {
		this.parentForm.get('hotelLoad')?.disable();
		this.setupFetchPipeline();
		this.watchSizeAndSpeedInputs();
		this.loadCategories();
	}

	ngOnDestroy(): void {
		this.destroy$.next();
		this.destroy$.complete();
	}

	private loadCategories(): void {
		this.appDataService.getCategories()
			.pipe(takeUntil(this.destroy$))
			.subscribe({
				next: (categories) => {
					this.categories = categories;
					if (this.categories.length > 0 && !this.selectedCategory) {
						this.applyCategorySelection(this.categories[0].name, false);
					}
					this.cdr.markForCheck();
				},
				error: () => {
					this.snackBar.open(
						'Unable to load vessel categories. Please check your connection and try again.',
						'Close',
						{ duration: 5000, panelClass: ['error-snackbar'] }
					);
				}
			});
	}

	onCategoryChange(categoryName: string): void {
		if (this.hasEditedFields()) {
			this.dialog.open(VesselConfigConfirmDialogComponent, {
				width: '420px',
				data: { vesselTypeName: categoryName },
				disableClose: true,
				backdropClass: 'dialog-backdrop'
			}).afterClosed().subscribe(result => {
				if (result === 'keep') {
					this.applyCategorySelection(categoryName, true);
				} else if (result === 'reset') {
					window.location.reload();
				}
				this.cdr.markForCheck();
			});
			return;
		}

		this.applyCategorySelection(categoryName, false);
	}

	private applyCategorySelection(categoryName: string, preserveEditedValues: boolean): void {
		const category = this.categories.find(c => c.name === categoryName) ?? null;
		this.selectedCategory = category;
		if (!category) {
			return;
		}

		this.applyCategoryValidators(category);
		this.parentForm.get('vesselCategory')?.setValue(category.name, { emitEvent: false });

		const sizeControl = this.parentForm.get('vesselSize');
		const speedControl = this.parentForm.get('vesselSpeedKnots');

		if (!preserveEditedValues) {
			const defaultSize = this.defaultSizeFor(category);
			const defaultSpeed = this.defaultSpeedFor(category);
			sizeControl?.setValue(defaultSize, { emitEvent: false });
			speedControl?.setValue(defaultSpeed, { emitEvent: false });
			this.editTracker.updateOriginalValue('vesselSize', defaultSize);
			this.editTracker.updateOriginalValue('vesselSpeedKnots', defaultSpeed);
			this.fetchTrigger$.next({ applyEngineDefaults: true, patchPowerFields: true });
		} else {
			// Keep the user's values; nudge them into the new category's valid ranges if needed
			if (this.selectedSize == null || this.selectedSize <= 0) {
				sizeControl?.setValue(this.defaultSizeFor(category), { emitEvent: false });
			}
			const speed = this.selectedSpeed;
			if (speed == null || speed < category.speedMin || speed > category.speedMax) {
				speedControl?.setValue(this.defaultSpeedFor(category), { emitEvent: false });
			}
			// Engine references must follow the category, but edited power/margin values stay
			this.fetchTrigger$.next({ applyEngineDefaults: false, patchPowerFields: false });
		}

		this.cdr.markForCheck();
	}

	private applyCategoryValidators(category: VesselCategoryData): void {
		const sizeControl = this.parentForm.get('vesselSize');
		// Any positive size is allowed; out-of-reference-range sizes clamp server-side (hint shown)
		sizeControl?.setValidators([Validators.required, Validators.min(1)]);
		sizeControl?.updateValueAndValidity({ emitEvent: false });

		const speedControl = this.parentForm.get('vesselSpeedKnots');
		speedControl?.setValidators([
			Validators.required,
			Validators.min(category.speedMin),
			Validators.max(category.speedMax)
		]);
		speedControl?.updateValueAndValidity({ emitEvent: false });
	}

	/** Midpoint of the reference range, rounded per unit (dwt -> 1000, TEU -> 100) */
	private defaultSizeFor(category: VesselCategoryData): number {
		const min = category.sizeMin ?? 0;
		// Unbounded categories (no upper reference): fall back to a deterministic pseudo-max
		const max = category.sizeMax ?? (min > 0 ? min * 2 : 20000);
		const mid = (min + max) / 2;
		const roundTo = category.unit.toUpperCase() === 'TEU' ? 100 : 1000;
		return Math.max(roundTo, Math.round(mid / roundTo) * roundTo);
	}

	/** Midpoint of the speed range, rounded to 0.5 kn */
	private defaultSpeedFor(category: VesselCategoryData): number {
		const mid = (category.speedMin + category.speedMax) / 2;
		return Math.round(mid * 2) / 2;
	}

	private watchSizeAndSpeedInputs(): void {
		const sizeChanges = this.parentForm.get('vesselSize')?.valueChanges;
		const speedChanges = this.parentForm.get('vesselSpeedKnots')?.valueChanges;
		if (!sizeChanges || !speedChanges) {
			return;
		}

		merge(sizeChanges, speedChanges)
			.pipe(takeUntil(this.destroy$))
			.subscribe(() => {
				this.fetchTrigger$.next({ applyEngineDefaults: false, patchPowerFields: true });
			});
	}

	private setupFetchPipeline(): void {
		this.fetchTrigger$
			.pipe(
				debounceTime(VesselConfigSectionComponent.FETCH_DEBOUNCE_MS),
				map(request => ({
					request,
					category: this.selectedCategory,
					size: this.selectedSize,
					speed: this.selectedSpeed
				})),
				filter((x): x is typeof x & { category: VesselCategoryData; size: number; speed: number } =>
					x.category != null
					&& x.size != null && x.size > 0
					&& x.speed != null
					&& x.speed >= x.category.speedMin && x.speed <= x.category.speedMax
				),
				switchMap(x =>
					this.appDataService.getFullVesselDataByCategory(x.category.name, x.size, x.speed).pipe(
						map(fullData => ({ fullData, request: x.request })),
						catchError(() => {
							this.snackBar.open(
								'Unable to load vessel configuration for the entered size and speed. Please try again.',
								'Close',
								{ duration: 5000, panelClass: ['error-snackbar'] }
							);
							return EMPTY;
						})
					)
				),
				takeUntil(this.destroy$)
			)
			.subscribe(({ fullData, request }) => this.applyVesselData(fullData, request));
	}

	private applyVesselData(fullData: FullVesselData, request: FetchRequest): void {
		const vesselConfig = fullData.vesselConfig;
		this.clampedToReferenceRange = fullData.resolution?.clamped === true;

		if (request.patchPowerFields) {
			this.parentForm.patchValue({
				propulsionPower: vesselConfig.calmWaterPowerKW,
				seaMargin: vesselConfig.seaMargin
			});
			this.editTracker.updateOriginalValue('propulsionPower', vesselConfig.calmWaterPowerKW);
			this.editTracker.updateOriginalValue('seaMargin', vesselConfig.seaMargin);
		}

		this.vesselEngineConfigSelected.emit({
			vesselType: vesselConfig,
			mainEngineData: fullData.mainEngineData,
			auxEngineData: fullData.auxEngineData,
			applyEngineDefaults: request.applyEngineDefaults
		});

		// The profile lives on the response; re-emit only when the source bucket changes
		// (size crossing a bucket boundary or a category change), so user edits to
		// operational fields are not overwritten on every size/speed keystroke.
		const profileSource = fullData.resolution?.profileSource ?? vesselConfig.vesselTypeName;
		const bucketChanged = profileSource !== this.lastProfileSource;
		if (request.patchPowerFields && fullData.operationalProfile && (request.applyEngineDefaults || bucketChanged)) {
			this.operationalProfileLoaded.emit(fullData.operationalProfile);
		}
		this.lastProfileSource = profileSource;

		this.cdr.markForCheck();
	}

	private hasEditedFields(): boolean {
		const fieldsToCheck = [
			'meCapacityPerEngine',
			'sgCapacityPerEngine',
			'aeCapacityPerEngine',
			'meCount',
			'aeCount',
			'propulsionPower',
			'seaMargin'
		];

		for (const fieldName of fieldsToCheck) {
			if (this.editTracker.isFieldEdited(fieldName, this.parentForm.get(fieldName)?.value)) {
				return true;
			}
		}

		return false;
	}

	isFieldEdited(fieldName: string): boolean {
		const control = this.parentForm.get(fieldName);
		if (!control) {
			return false;
		}

		return this.editTracker.isFieldEdited(fieldName, control.value);
	}

	reloadPage(): void {
		window.location.reload();
	}

	/** Programmatic selection used when restoring a saved profile or draft */
	selectVessel(categoryName: string, size: number, speed: number): void {
		this.appDataService.getCategories()
			.pipe(takeUntil(this.destroy$))
			.subscribe({
				next: (categories) => {
					this.categories = categories;
					const category = categories.find(c => c.name === categoryName) ?? null;
					if (!category) {
						return;
					}
					this.selectedCategory = category;
					this.applyCategoryValidators(category);
					this.parentForm.get('vesselCategory')?.setValue(category.name, { emitEvent: false });
					this.parentForm.get('vesselSize')?.setValue(size, { emitEvent: false });
					this.parentForm.get('vesselSpeedKnots')?.setValue(speed, { emitEvent: false });
					// Programmatic restore is the baseline, not a user edit — keep the "(edited)" badge off.
					this.editTracker.updateOriginalValue('vesselSize', size);
					this.editTracker.updateOriginalValue('vesselSpeedKnots', speed);
					this.fetchTrigger$.next({ applyEngineDefaults: true, patchPowerFields: true });
					this.cdr.markForCheck();
				},
				error: () => {
					this.snackBar.open(
						'Unable to restore the saved vessel selection.',
						'Close',
						{ duration: 5000, panelClass: ['error-snackbar'] }
					);
				}
			});
	}

	trackByCategory(index: number, category: VesselCategoryData): string {
		return category.name;
	}
}
