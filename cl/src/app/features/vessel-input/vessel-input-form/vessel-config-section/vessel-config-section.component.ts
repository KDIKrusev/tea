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
import { FormInputFieldComponent } from '../../../../shared/components';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { VesselConfigConfirmDialogComponent } from './vessel-config-confirm-dialog.component';
import { AppDataService } from '../../../../core/app-data.service';
import { FullVesselData, VesselCategoryData } from '../../../../core/app-data.types';
import { InterpolatedVesselConfig } from '../../../../core/vessel-configuration.types';
import { EngineType, AuxiliaryEngineType } from '../../../../core/engine-configuration.types';
import { VesselOperationalProfile } from '../../../../core/operational-profile.types';
import { FormEditTrackerService } from '../form-edit-tracker.service';
import { NotificationService } from '../../../../shared/services';

/** What to do with a fetched vessel config (mirrors the old vesselTypeJustChanged semantics) */
interface FetchRequest {
	applyEngineDefaults: boolean;
	patchPowerFields: boolean;
}

/** The vessel selection a fetch was issued for. Identity, not just parameters — see applyVesselData. */
interface Selection {
	category: string;
	size: number;
	speed: number;
}

function sameSelection(a: Selection | null, b: Selection | null): boolean {
	if (a === null || b === null) {
		return false;
	}
	return a.category === b.category && a.size === b.size && a.speed === b.speed;
}

/**
 * One applied vessel-config response.
 *
 * `operationalProfile` is part of the message rather than a second, conditional event: the parent
 * has to know whether a profile is coming *before* it decides what to do, and it cannot know that
 * from an event that may or may not follow.
 */
export interface VesselDataApplied {
	vesselType: InterpolatedVesselConfig;
	mainEngineData?: EngineType;
	auxEngineData?: AuxiliaryEngineType;
	applyEngineDefaults: boolean;
	/** null when the response carried no profile, or when the source bucket did not change. */
	operationalProfile: VesselOperationalProfile | null;
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
		MatDialogModule,
		VesselConfigConfirmDialogComponent
	],
	changeDetection: ChangeDetectionStrategy.OnPush,
	templateUrl: './vessel-config-section.component.html',
	styleUrl: './vessel-config-section.component.css'
})
export class VesselConfigSectionComponent implements OnInit, OnDestroy {
	@Input() parentForm!: FormGroup;
	/** One vessel-config response, fully described. See applyVesselData for why it is one event. */
	@Output() vesselDataApplied = new EventEmitter<VesselDataApplied>();
	/** The fetch failed — nothing was applied, and anyone waiting on it should stop waiting. */
	@Output() vesselDataFailed = new EventEmitter<void>();

	private destroy$ = new Subject<void>();
	private appDataService = inject(AppDataService);
	protected editTracker = inject(FormEditTrackerService);
	private notify = inject(NotificationService);
	private cdr = inject(ChangeDetectorRef);
	private dialog = inject(MatDialog);

	categories: VesselCategoryData[] = [];
	selectedCategory: VesselCategoryData | null = null;

	/** True when the backend clamped the size to the nearest reference curve */
	clampedToReferenceRange = false;

	private fetchTrigger$ = new Subject<FetchRequest>();
	private lastProfileSource: string | null = null;

	/**
	 * Deliberate UX debounce — one of only two timing constants left in the client (the other is
	 * DEBOUNCE_TIMES.FORM_INPUT). It exists so that typing a five-digit vessel size does not fire
	 * five HTTP requests, NOT to sequence anything. Nothing downstream may depend on its value:
	 * a response is matched to its selection, and a load ends when its response is applied.
	 */
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
					this.notify.error('Unable to load vessel categories. Please check your connection and try again.');
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
				switchMap(x => {
					const selection: Selection = { category: x.category.name, size: x.size, speed: x.speed };
					return this.appDataService.getFullVesselDataByCategory(x.category.name, x.size, x.speed).pipe(
						map(fullData => ({ fullData, request: x.request, selection })),
						catchError(() => {
							this.notify.error('Unable to load vessel configuration for the entered size and speed. Please try again.');
							// The parent may be waiting on this response to finish a load sequence —
							// tell it the wait is over instead of leaving it pending forever.
							this.vesselDataFailed.emit();
							return EMPTY;
						})
					);
				}),
				takeUntil(this.destroy$)
			)
			.subscribe(({ fullData, request, selection }) => this.applyVesselData(fullData, request, selection));
	}

	/** The selection currently shown in the form, or null while it is incomplete. */
	private currentSelection(): Selection | null {
		const category = this.selectedCategoryName;
		const size = this.selectedSize;
		const speed = this.selectedSpeed;
		if (category === null || size === null || speed === null) {
			return null;
		}
		return { category, size, speed };
	}

	private applyVesselData(fullData: FullVesselData, request: FetchRequest, selection: Selection): void {
		// A response is only valid for the selection it was asked for.
		//
		// `switchMap` cancels an in-flight request when a NEW trigger fires — but a trigger spends
		// 400 ms in the debounce before it reaches the switchMap, and a response that lands inside
		// that window is not cancelled. That window is the whole bug: a user who clicks Load while
		// the startup fetch is still on the wire gets their restore completed by a response fetched
		// for the previous vessel, and the restore's own response then arrives after the restore has
		// already declared itself finished — and overwrites the profile with the vessel type's
		// defaults. Dropping the mismatched response closes the window at its source.
		if (!sameSelection(selection, this.currentSelection())) {
			return;
		}

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

		// The profile lives on the response; re-apply only when the source bucket changes
		// (size crossing a bucket boundary or a category change), so user edits to
		// operational fields are not overwritten on every size/speed keystroke.
		const profileSource = fullData.resolution?.profileSource ?? vesselConfig.vesselTypeName;
		const bucketChanged = profileSource !== this.lastProfileSource;
		const carriesProfile =
			request.patchPowerFields
			&& !!fullData.operationalProfile
			&& (request.applyEngineDefaults || bucketChanged);
		this.lastProfileSource = profileSource;

		// ONE event, not two.
		//
		// This used to emit `vesselEngineConfigSelected` and then, conditionally,
		// `operationalProfileLoaded`. The parent could not tell inside the first handler whether the
		// second would follow, so it armed an 800 ms timer to apply the profile "in case nothing
		// else does". Carrying the profile as a nullable field on one event makes the answer part of
		// the message, and the timer unnecessary.
		this.vesselDataApplied.emit({
			vesselType: vesselConfig,
			mainEngineData: fullData.mainEngineData,
			auxEngineData: fullData.auxEngineData,
			applyEngineDefaults: request.applyEngineDefaults,
			operationalProfile: carriesProfile ? fullData.operationalProfile : null
		});

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
					this.notify.error('Unable to restore the saved vessel selection.');
				}
			});
	}

	trackByCategory(_index: number, category: VesselCategoryData): string {
		return category.name;
	}
}
