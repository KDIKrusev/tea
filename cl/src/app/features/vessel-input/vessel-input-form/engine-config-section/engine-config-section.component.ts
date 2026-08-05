import { Component, Input, OnInit, OnDestroy, inject, ChangeDetectorRef, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AbstractControl, FormGroup, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatButtonModule } from '@angular/material/button';
import { AppDataService } from '../../../../core/app-data.service';
import { MatDialogModule, MatDialog } from '@angular/material/dialog';
import { EngineType, AuxiliaryEngineType } from '../../../../core/engine-configuration.types';
import { FormEditTrackerService } from '../form-edit-tracker.service';
import { NotificationService } from '../../../../shared/services';
import { EngineConfigConfirmDialogComponent } from './engine-config-confirm-dialog.component';
import { fuelsForFamily, FuelType } from '../../../../shared/constants';

@Component({
	selector: 'app-engine-config-section',
	standalone: true,
	imports: [
		CommonModule,
		ReactiveFormsModule,
		FormsModule,
		MatFormFieldModule,
		MatInputModule,
		MatIconModule,
		MatSelectModule,
		MatExpansionModule,
		MatButtonModule,
		MatDialogModule,
		EngineConfigConfirmDialogComponent
	],
	changeDetection: ChangeDetectionStrategy.OnPush,
	templateUrl: './engine-config-section.component.html',
	styleUrl: './engine-config-section.component.css'
})
export class EngineConfigSectionComponent implements OnInit, OnDestroy {
	@Input() parentForm!: FormGroup;

	private destroy$ = new Subject<void>();
	private appDataService = inject(AppDataService);
	private cdr = inject(ChangeDetectorRef);
	protected editTracker = inject(FormEditTrackerService);
	private notify = inject(NotificationService);
	private dialog = inject(MatDialog);

	mainEngineTypes: EngineType[] = [];
	auxiliaryEngineTypes: AuxiliaryEngineType[] = [];

	// Maker-grouped + searchable dropdown state (Story 3.5)
	mainEngineGroups: Array<{ maker: string; engines: EngineType[] }> = [];
	auxEngineGroups: Array<{ maker: string; engines: AuxiliaryEngineType[] }> = [];
	filteredMainEngineGroups: Array<{ maker: string; engines: EngineType[] }> = [];
	filteredAuxEngineGroups: Array<{ maker: string; engines: AuxiliaryEngineType[] }> = [];
	mainEngineSearch = '';
	auxEngineSearch = '';

	selectedMainEngineTypeId: number | null = null;
	selectedAuxiliaryEngineTypeId: number | null = null;

	ngOnInit(): void {
		this.loadEngineConfigurations();
		this.setupValueChangeTracking();
	}

	ngOnDestroy(): void {
		this.destroy$.next();
		this.destroy$.complete();
	}

	private setupValueChangeTracking(): void {
		this.parentForm.get('meCapacityPerEngine')?.valueChanges
			.pipe(takeUntil(this.destroy$)).subscribe(() => this.cdr.markForCheck());
		this.parentForm.get('sgCapacityPerEngine')?.valueChanges
			.pipe(takeUntil(this.destroy$)).subscribe(() => this.cdr.markForCheck());
		this.parentForm.get('aeCapacityPerEngine')?.valueChanges
			.pipe(takeUntil(this.destroy$)).subscribe(() => this.cdr.markForCheck());
	}

	/**
	 * Loads the catalogue and pre-fills the first entry so the dropdowns are never empty.
	 *
	 * There is no "replay a selection that arrived before the catalogue" branch any more. It could
	 * not fire: both callers of `setEngineConfiguration`/`setEngineTypeReferences` run from a
	 * vessel-config response, which is issued only after the categories — from this same catalogue
	 * payload — have arrived, and then 400 ms behind a debounce. `catalogue-precedes-vessel-config.spec.ts`
	 * pins that invariant, so the branch is proved unnecessary rather than argued away.
	 */
	loadEngineConfigurations(): void {
		this.appDataService.getMainEngineTypes()
			.pipe(takeUntil(this.destroy$))
			.subscribe({
				next: (mainEngines) => {
					this.mainEngineTypes = mainEngines;
					this.mainEngineGroups = this.buildGroups(this.mainEngineTypes);
					this.applyMainFilter();
					if (this.mainEngineTypes.length > 0 && !this.selectedMainEngineTypeId) {
						this.selectedMainEngineTypeId = this.mainEngineTypes[0].id;
						this.applyMainEngineChange(this.mainEngineTypes[0]);
					}
					this.cdr.markForCheck();
				},
				error: () => {
					this.notify.error('Unable to load main engine configurations. Please check your connection and try again.');
				}
			});

		this.appDataService.getAuxiliaryEngineTypes()
			.pipe(takeUntil(this.destroy$))
			.subscribe({
				next: (auxEngines) => {
					this.auxiliaryEngineTypes = auxEngines;
					this.auxEngineGroups = this.buildGroups(this.auxiliaryEngineTypes);
					this.applyAuxFilter();
					if (this.auxiliaryEngineTypes.length > 0 && !this.selectedAuxiliaryEngineTypeId) {
						this.selectedAuxiliaryEngineTypeId = this.auxiliaryEngineTypes[0].id;
						this.applyAuxEngineChange(this.auxiliaryEngineTypes[0]);
					}
					this.cdr.markForCheck();
				},
				error: () => {
					this.notify.error('Unable to load auxiliary engine configurations. Please check your connection and try again.');
				}
			});
	}

	// ─── Main Engine ─────────────────────────────────────────────────────────────

	onMainEngineTypeChange(engineTypeId: number): void {
		const selectedEngine = this.mainEngineTypes.find(e => e.id === engineTypeId);
		if (!selectedEngine) {return;}

		const hasEditedCapacity =
			this.editTracker.isFieldEdited('meCapacityPerEngine', this.parentForm.get('meCapacityPerEngine')?.value) ||
			this.editTracker.isFieldEdited('sgCapacityPerEngine', this.parentForm.get('sgCapacityPerEngine')?.value);

		if (hasEditedCapacity) {
			this.dialog.open(EngineConfigConfirmDialogComponent, {
				width: '420px',
				data: {
					engineName: selectedEngine.name,
					engineType: 'Main Engine',
					defaultMainCapacityKw: selectedEngine.maxCapacityKW,
					defaultShaftGeneratorCapacityKw: selectedEngine.shaftGeneratorMaxCapacityKW
				},
				disableClose: true,
				backdropClass: 'dialog-backdrop'
			}).afterClosed().subscribe(result => {
				if (result === 'reset') {
					this.applyMainEngineChange(selectedEngine);
				} else if (result === 'keep') {
					this.selectedMainEngineTypeId = engineTypeId;
					this.parentForm.patchValue({ mainEngineTypeId: engineTypeId });
					this.editTracker.updateOriginalValue('mainEngineTypeId', engineTypeId);
					this.reconcileMainFuel(selectedEngine);
				}
				this.cdr.markForCheck();
			});
		} else {
			this.applyMainEngineChange(selectedEngine);
		}
	}


	private applyMainEngineChange(engine: EngineType): void {
		this.selectedMainEngineTypeId = engine.id;

		// Shaft-generator capacity is installation-specific and is NOT part of the imported
		// engine catalogue (only the legacy engines carry a nominal SG). So only pre-fill SG
		// when the selected model actually provides one — otherwise keep the user's current
		// value instead of wiping the field to empty.
		const patch: Record<string, unknown> = {
			meCapacityPerEngine: engine.maxCapacityKW,
			mainEngineTypeId: engine.id
		};
		if (engine.shaftGeneratorMaxCapacityKW != null) {
			patch['sgCapacityPerEngine'] = engine.shaftGeneratorMaxCapacityKW;
		}
		this.parentForm.patchValue(patch);

		this.editTracker.updateOriginalValue('meCapacityPerEngine', engine.maxCapacityKW);
		if (engine.shaftGeneratorMaxCapacityKW != null) {
			this.editTracker.updateOriginalValue('sgCapacityPerEngine', engine.shaftGeneratorMaxCapacityKW);
		}
		this.editTracker.updateOriginalValue('mainEngineTypeId', engine.id);
		this.reconcileMainFuel(engine);
		// Reached by a direct method call from the parent (setEngineConfiguration). Under OnPush the
		// header is built from getSelectedMainEngineName() and the fuel helpers, all method calls in
		// the template, so without this they keep showing the previous engine. It happens to work
		// today because patching the capacity fires valueChanges into a markForCheck subscription —
		// a side effect, not a guarantee. rendered-output.spec.ts pins it.
		this.cdr.markForCheck();
	}

	// ─── Auxiliary Engine ─────────────────────────────────────────────────────────

	onAuxiliaryEngineTypeChange(engineTypeId: number): void {
		const selectedEngine = this.auxiliaryEngineTypes.find(e => e.id === engineTypeId);
		if (!selectedEngine) {return;}

		const hasEditedCapacity =
			this.editTracker.isFieldEdited('aeCapacityPerEngine', this.parentForm.get('aeCapacityPerEngine')?.value);

		if (hasEditedCapacity) {
			this.dialog.open(EngineConfigConfirmDialogComponent, {
				width: '420px',
				data: {
					engineName: selectedEngine.name,
					engineType: 'Auxiliary Engine',
					defaultAuxCapacityKw: selectedEngine.maxCapacityKW
				},
				disableClose: true,
				backdropClass: 'dialog-backdrop'
			}).afterClosed().subscribe(result => {
				if (result === 'reset') {
					this.applyAuxEngineChange(selectedEngine);
				} else if (result === 'keep') {
					this.selectedAuxiliaryEngineTypeId = engineTypeId;
					this.parentForm.patchValue({ auxEngineTypeId: engineTypeId });
					this.editTracker.updateOriginalValue('auxEngineTypeId', engineTypeId);
					this.reconcileAuxFuel(selectedEngine);
				}
				this.cdr.markForCheck();
			});
		} else {
			this.applyAuxEngineChange(selectedEngine);
		}
	}


	private applyAuxEngineChange(engine: AuxiliaryEngineType): void {
		this.selectedAuxiliaryEngineTypeId = engine.id;
		this.parentForm.patchValue({
			aeCapacityPerEngine: engine.maxCapacityKW,
			auxEngineTypeId: engine.id
		});
		this.editTracker.updateOriginalValue('aeCapacityPerEngine', engine.maxCapacityKW);
		this.editTracker.updateOriginalValue('auxEngineTypeId', engine.id);
		this.reconcileAuxFuel(engine);
		this.cdr.markForCheck();
	}

	// ─── Vessel-type-driven config (bypasses confirmation) ────────────────────────

	/** The vessel type's engines: ids AND rated capacities. */
	setEngineConfiguration(mainEngineId: number, auxiliaryEngineId: number): void {
		const mainEngineTypeId = Number(mainEngineId);
		const auxEngineTypeId = Number(auxiliaryEngineId);
		if (!Number.isFinite(mainEngineTypeId) || !Number.isFinite(auxEngineTypeId)) {
			return;
		}

		const mainEngine = this.mainEngineTypes.find(e => e.id === mainEngineTypeId);
		const auxEngine = this.auxiliaryEngineTypes.find(e => e.id === auxEngineTypeId);
		if (mainEngine) {
			this.applyMainEngineChange(mainEngine);
		}
		if (auxEngine) {
			this.applyAuxEngineChange(auxEngine);
		}
		this.editTracker.updateOriginalValue('meCount', this.parentForm.get('meCount')?.value);
		this.editTracker.updateOriginalValue('aeCount', this.parentForm.get('aeCount')?.value);
	}

	/** Ids only — the caller (a restored profile) owns the capacities. */
	setEngineTypeReferences(mainEngineId: number, auxiliaryEngineId: number): void {
		const mainEngineTypeId = Number(mainEngineId);
		const auxEngineTypeId = Number(auxiliaryEngineId);
		if (!Number.isFinite(mainEngineTypeId) || !Number.isFinite(auxEngineTypeId)) {
			return;
		}

		this.applyEngineReferences(mainEngineTypeId, auxEngineTypeId);
	}

	/**
	 * Ids and fuel only — the rated capacities stay as they are. Used when the caller is the
	 * authority on capacity (a restored profile), so the catalogue must not overwrite it.
	 */
	private applyEngineReferences(mainEngineTypeId: number, auxEngineTypeId: number): void {
		const mainEngine = this.mainEngineTypes.find(e => e.id === mainEngineTypeId);
		const auxEngine = this.auxiliaryEngineTypes.find(e => e.id === auxEngineTypeId);

		// Always sync selectedEngineTypeId to the saved ID so the dropdown reflects
		// the profile value. If the engine is found, also reconcile the fuel;
		// if not found the dropdown may show blank until the user re-selects, which is
		// preferable to silently showing the wrong vessel-default engine.
		this.selectedMainEngineTypeId = mainEngineTypeId;
		this.parentForm.patchValue({ mainEngineTypeId: mainEngineTypeId });
		this.editTracker.updateOriginalValue('mainEngineTypeId', mainEngineTypeId);
		if (mainEngine) {
			this.reconcileMainFuel(mainEngine);
		}

		this.selectedAuxiliaryEngineTypeId = auxEngineTypeId;
		this.parentForm.patchValue({ auxEngineTypeId: auxEngineTypeId });
		this.editTracker.updateOriginalValue('auxEngineTypeId', auxEngineTypeId);
		if (auxEngine) {
			this.reconcileAuxFuel(auxEngine);
		}

		this.cdr.markForCheck();
	}

	// ─── Getters / helpers ────────────────────────────────────────────────────────

	get meCapacityPerEngine(): AbstractControl | null { return this.parentForm.get('meCapacityPerEngine'); }
	get meCount(): AbstractControl | null { return this.parentForm.get('meCount'); }
	get sgCapacityPerEngine(): AbstractControl | null { return this.parentForm.get('sgCapacityPerEngine'); }
	get aeCapacityPerEngine(): AbstractControl | null { return this.parentForm.get('aeCapacityPerEngine'); }
	get aeCount(): AbstractControl | null { return this.parentForm.get('aeCount'); }

	isFieldEdited(fieldName: string): boolean {
		const control = this.parentForm.get(fieldName);
		if (!control) {return false;}
		return this.editTracker.isFieldEdited(fieldName, control.value);
	}

	getValidationError(controlName: string): string {
		const control = this.parentForm.get(controlName);
		if (!control) {return '';}
		if (control.hasError('required')) {return 'This field is required';}
		if (control.hasError('min')) {return `Minimum value is ${control.errors?.['min'].min}`;}
		if (control.hasError('max')) {return `Maximum value is ${control.errors?.['max'].max}`;}
		return '';
	}

	getSelectedMainEngineName(): string {
		if (!this.selectedMainEngineTypeId) {return '';}
		const e = this.mainEngineTypes.find(x => x.id === this.selectedMainEngineTypeId);
		return e ? this.mainEngineLabel(e) : '';
	}

	getSelectedAuxiliaryEngineName(): string {
		if (!this.selectedAuxiliaryEngineTypeId) {return '';}
		const e = this.auxiliaryEngineTypes.find(x => x.id === this.selectedAuxiliaryEngineTypeId);
		return e ? this.auxEngineLabel(e) : '';
	}

	// ─── Maker grouping + search (Story 3.5) ──────────────────────────────────────

	private buildGroups<T extends { maker?: string | null; name: string }>(list: T[]): Array<{ maker: string; engines: T[] }> {
		const groups = new Map<string, T[]>();
		for (const e of list) {
			const key = e.maker ?? 'Other';
			if (!groups.has(key)) {groups.set(key, []);}
			groups.get(key)!.push(e);
		}
		return [...groups.entries()]
			// alphabetical by maker, with "Other" (legacy/null-maker) pinned last
			.sort((a, b) => a[0] === 'Other' ? 1 : b[0] === 'Other' ? -1 : a[0].localeCompare(b[0]))
			.map(([maker, engines]) => ({ maker, engines: [...engines].sort((x, y) => x.name.localeCompare(y.name)) }));
	}

	private filterGroups<T extends { maker?: string | null; name: string; fuelFamily?: string | null }>(
		groups: Array<{ maker: string; engines: T[] }>, term: string): Array<{ maker: string; engines: T[] }> {
		const t = term.trim().toLowerCase();
		if (!t) {return groups;}
		// Match maker, model name AND fuel family so typing "LNG"/"Ammonia"/"Liquid" surfaces compatible engines.
		return groups
			.map(g => ({ maker: g.maker, engines: g.engines.filter(e => `${e.maker ?? ''} ${e.name} ${e.fuelFamily ?? ''}`.toLowerCase().includes(t)) }))
			.filter(g => g.engines.length > 0);
	}

	applyMainFilter(): void {
		this.filteredMainEngineGroups = this.filterGroups(this.mainEngineGroups, this.mainEngineSearch);
		this.cdr.markForCheck();
	}

	applyAuxFilter(): void {
		this.filteredAuxEngineGroups = this.filterGroups(this.auxEngineGroups, this.auxEngineSearch);
		this.cdr.markForCheck();
	}

	mainEngineLabel(e: EngineType): string { return e.maker ? `${e.maker} ${e.name}` : e.name; }
	auxEngineLabel(e: AuxiliaryEngineType): string { return e.maker ? `${e.maker} ${e.name}` : e.name; }

	// ─── Per-engine fuel selection (Story 3.6) ────────────────────────────────────

	mainFuelOptions(): FuelType[] {
		const e = this.mainEngineTypes.find(x => x.id === this.selectedMainEngineTypeId);
		return fuelsForFamily(e?.fuelFamily);
	}
	auxFuelOptions(): FuelType[] {
		const e = this.auxiliaryEngineTypes.find(x => x.id === this.selectedAuxiliaryEngineTypeId);
		return fuelsForFamily(e?.fuelFamily);
	}
	isMainFuelLocked(): boolean { return this.mainFuelOptions().length <= 1; }
	isAuxFuelLocked(): boolean { return this.auxFuelOptions().length <= 1; }

	selectedMainFuel(): string { return this.parentForm.get('mainFuelType')?.value ?? ''; }
	selectedAuxFuel(): string { return this.parentForm.get('auxFuelType')?.value ?? ''; }

	mainFuelHint(): string {
		if (!this.isMainFuelLocked()) {
			return 'MGO / MDO / HFO — sets CO₂ & default price';
		}

		const engine = this.mainEngineTypes.find(x => x.id === this.selectedMainEngineTypeId);
		if (engine?.name?.toUpperCase().includes('-MEG')) {
			return 'LNG-only model. For liquid fuel, choose the sibling model without -MEG.';
		}

		return 'Fixed by engine type';
	}

	onMainFuelChange(): void {
		const fuel = this.parentForm.get('mainFuelType')?.value;
		this.editTracker.updateOriginalValue('mainFuelType', fuel);
		this.prefillPriceFromMainFuel(fuel);
	}
	onAuxFuelChange(): void {
		this.editTracker.updateOriginalValue('auxFuelType', this.parentForm.get('auxFuelType')?.value);
	}

	// When the engine (family) changes, keep the chosen fuel valid; otherwise reset to the family's first fuel.
	private reconcileMainFuel(engine: EngineType): void {
		const options = fuelsForFamily(engine.fuelFamily);
		const current = this.parentForm.get('mainFuelType')?.value as FuelType | null;
		const fuel = current && options.includes(current) ? current : options[0];
		this.parentForm.patchValue({ mainFuelType: fuel });
		this.editTracker.updateOriginalValue('mainFuelType', fuel);
		this.prefillPriceFromMainFuel(fuel);
	}
	private reconcileAuxFuel(engine: AuxiliaryEngineType): void {
		const options = fuelsForFamily(engine.fuelFamily);
		const current = this.parentForm.get('auxFuelType')?.value as FuelType | null;
		const fuel = current && options.includes(current) ? current : options[0];
		this.parentForm.patchValue({ auxFuelType: fuel });
		this.editTracker.updateOriginalValue('auxFuelType', fuel);
	}

	// Single-price model: the Main engine fuel drives the default fuel price (editable afterwards).
	// The price comes from the backend (AppInitialData.fuelDefaultPrices) — the client keeps no copy,
	// because the copy it used to keep had drifted from the backend on every fuel.
	private prefillPriceFromMainFuel(fuel: string): void {
		const price = this.appDataService.getFuelDefaultPrices()[fuel];
		if (price != null) {
			this.parentForm.patchValue({ fuelPrice: price });
			// Re-baseline the edit tracker: this default is not a user edit, so a later default may
			// still replace it.
			this.editTracker.updateOriginalValue('fuelPrice', price);
		}
	}

	engineSubtitle(e: { fuelFamily?: string | null; ratedPowerKW?: number | null; maxCapacityKW: number }): string {
		const power = e.ratedPowerKW ?? e.maxCapacityKW;
		return e.fuelFamily ? `${e.fuelFamily} · ${power} kW` : `${power} kW`;
	}

	trackByMaker(_position: number, group: { maker: string }): string {
		return group.maker;
	}

	trackByFuel(_position: number, fuel: FuelType): string {
		return fuel;
	}

	trackByEngineType(_index: number, engineType: { id: number; name: string }): number {
		return engineType.id;
	}
}
