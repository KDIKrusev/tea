/**
 * `RestoreHarness` — the real component tree, on the spec's clock.
 *
 * Mounts `VesselInputFormComponent` (or the whole `CalculatorPageComponent`) inside `fakeAsync`,
 * with `ApiFixture` holding both HTTP responses. Nothing is mocked away: the real vessel-config
 * section, engine-config section, operational-modes section and battery section all run, because
 * the bug this harness exists for lives in the interaction BETWEEN them.
 *
 * Story C-A · docs/stories/brownfield-client-a-test-harness.md
 */
import { Type } from '@angular/core';
import { ComponentFixture, TestBed, discardPeriodicTasks, flush, tick } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';

import { ApiFixture, TEST_API_URL } from './api-fixture';
import { ScenarioFile, asSavedProfile } from './scenarios';
import { ConfigService } from '../app/core/config.service';
import { SavedProfile } from '../app/core/profile.types';
import {
  FormChangeEvent,
  VesselInputFormComponent,
} from '../app/features/vessel-input/vessel-input-form/vessel-input-form.component';
import { CalculatorPageComponent } from '../app/features/calculator-page/calculator-page.component';
import { FormInputFieldComponent } from '../app/shared/components';

const PROFILES_KEY = 'ksailcalc_profiles';
const DRAFT_KEY = 'ksailcalc_draft';

/**
 * How far `settle()` advances the clock.
 *
 * Longer than every timing constant the client currently owns, so a settled state is genuinely
 * settled and not a snapshot taken between two writers:
 *
 *   200 ms  applyProfileInputValues delay          vessel-input-form.component.ts:599
 *   400 ms  vessel-config fetch debounce           vessel-config-section.component.ts:70
 *   500 ms  form valueChanges debounce             DEBOUNCE_TIMES.FORM_INPUT
 *   800 ms  profile-apply fallback                 vessel-input-form.component.ts:544
 *  1500 ms  initial emission fallback              vessel-input-form.component.ts:227
 *  3000 ms  restore watchdog                       RESTORE_WATCHDOG_MS
 *
 * When C-E removes those constants this number shrinks with them — it is a mirror of the current
 * design, not a target.
 */
export const SETTLE_MS = 6000;

export type HarnessTarget = 'form' | 'page';

export interface HarnessOptions {
  /** Seed a saved auto-draft before mounting — the hard-refresh entry point. */
  draft?: ScenarioFile;
  /** Seed the saved-profiles list before mounting. */
  profiles?: SavedProfile[];
}

export class RestoreHarness {
  readonly api: ApiFixture;

  private readonly emitted: FormChangeEvent[] = [];
  private pageFixture: ComponentFixture<CalculatorPageComponent> | null = null;
  private formFixture: ComponentFixture<VesselInputFormComponent> | null = null;
  private disposed = false;

  private constructor(httpMock: HttpTestingController) {
    this.api = new ApiFixture(httpMock);
  }

  // ─── Mounting ───────────────────────────────────────────────────────────────

  /**
   * `VesselInputFormComponent` + its real child sections. Use for form-state assertions.
   *
   * NOTE: the app's own `appConfig` is deliberately NOT bootstrapped — its `APP_INITIALIZER`
   * calls the browser `fetch()` for `/config.json`, which `HttpTestingController` does not
   * intercept and which would hang the spec.
   */
  static mountForm(options: HarnessOptions = {}): RestoreHarness {
    const harness = RestoreHarness.configure(options);
    harness.formFixture = TestBed.createComponent(VesselInputFormComponent);
    harness.formComponent.formChanged.subscribe(e => harness.emitted.push(e));
    harness.detectChanges(); // ngOnInit + ngAfterViewInit ⇒ the cascade starts here
    return harness;
  }

  /** The whole page. Use when the assertion is about POST bodies or the pinned baseline. */
  static mountPage(options: HarnessOptions = {}): RestoreHarness {
    const harness = RestoreHarness.configure(options);
    harness.pageFixture = TestBed.createComponent(CalculatorPageComponent);
    harness.detectChanges();
    harness.formComponent.formChanged.subscribe(e => harness.emitted.push(e));
    return harness;
  }

  private static configure(options: HarnessOptions): RestoreHarness {
    RestoreHarness.clearStorage();
    if (options.profiles) {
      localStorage.setItem(PROFILES_KEY, JSON.stringify(options.profiles));
    }
    if (options.draft) {
      localStorage.setItem(
        DRAFT_KEY,
        JSON.stringify({
          input: options.draft.input,
          vesselTypeName: options.draft.vesselTypeName,
          vesselCategory: options.draft.vesselCategory,
          vesselSize: options.draft.vesselSize,
          vesselSpeed: options.draft.vesselSpeed,
          savedAt: '2026-01-01T00:00:00.000Z',
        }),
      );
    }

    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideNoopAnimations()],
    });
    TestBed.inject(ConfigService).setConfig({ apiUrl: TEST_API_URL });

    return new RestoreHarness(TestBed.inject(HttpTestingController));
  }

  /** localStorage survives between specs in the same browser — always start from empty. */
  static clearStorage(): void {
    localStorage.removeItem(PROFILES_KEY);
    localStorage.removeItem(DRAFT_KEY);
  }

  // ─── Access ─────────────────────────────────────────────────────────────────

  get formComponent(): VesselInputFormComponent {
    if (this.formFixture) {
      return this.formFixture.componentInstance;
    }
    if (this.pageFixture) {
      return this.query(this.pageFixture, VesselInputFormComponent);
    }
    throw new Error('RestoreHarness: nothing is mounted');
  }

  get pageComponent(): CalculatorPageComponent {
    if (!this.pageFixture) {
      throw new Error('RestoreHarness: mountPage() was not used');
    }
    return this.pageFixture.componentInstance;
  }

  private query<T>(fixture: ComponentFixture<unknown>, type: Type<T>): T {
    const found = fixture.debugElement.query(By.directive(type as Type<unknown>));
    if (!found) {
      throw new Error(`RestoreHarness: ${type.name} is not in the mounted tree`);
    }
    return found.componentInstance as T;
  }

  /** Every control, including the disabled `hotelLoad`. */
  formValue(): Record<string, unknown> {
    return this.formComponent.vesselForm.getRawValue() as Record<string, unknown>;
  }

  /**
   * The rendered DOM of the mounted tree.
   *
   * Most specs here assert form state or request bodies; this is for the ones that have to check
   * what actually reached the screen. OnPush makes those two things able to disagree.
   */
  get formElement(): HTMLElement {
    const fixture = this.pageFixture ?? this.formFixture;
    if (!fixture) {
      throw new Error('RestoreHarness: nothing is mounted');
    }
    return fixture.nativeElement as HTMLElement;
  }

  /**
   * The rendered element of one form section, by its selector.
   *
   * Queries MUST be scoped this way. Six sections share the same `.info-badge` class, so an
   * unscoped `querySelector('.info-badge')` matches the vessel-config header — a spec written that
   * way passes whatever the section under test is doing.
   */
  section(selector: string): HTMLElement {
    const el = this.formElement.querySelector(selector);
    if (!el) {
      throw new Error(`RestoreHarness: <${selector}> is not in the mounted tree`);
    }
    return el as HTMLElement;
  }

  /**
   * The rendered `<input>` for a form control.
   *
   * `[formControlName]="controlName"` is a property binding, so there is no `formcontrolname`
   * attribute in the DOM to select on. The control name lives on the `FormInputFieldComponent`
   * instance, so that is what gets matched.
   */
  inputFor(controlName: string): HTMLInputElement | null {
    const fixture = this.pageFixture ?? this.formFixture;
    if (!fixture) {
      throw new Error('RestoreHarness: nothing is mounted');
    }
    const field = fixture.debugElement
      .queryAll(By.directive(FormInputFieldComponent))
      .find(candidate => (candidate.componentInstance as FormInputFieldComponent).controlName === controlName);
    return field ? (field.nativeElement as HTMLElement).querySelector('input') : null;
  }

  /** The "12.3%" badges next to each operational mode, as rendered. */
  modeSharePercentages(): string[] {
    return Array.from(
      this.section('app-operational-modes-section').querySelectorAll('.percentage-badge')
    ).map(el => el.textContent?.trim() ?? '');
  }

  field(name: string): unknown {
    return this.formValue()[name];
  }

  /** Emissions in order, with the `source` tag the component chose for each. */
  emissions(): readonly FormChangeEvent[] {
    return this.emitted;
  }

  emissionSources(): string[] {
    return this.emitted.map(e => e.source);
  }

  // ─── Driving ────────────────────────────────────────────────────────────────

  /** The "Load" button path: the profile manager hands a `SavedProfile` to the page/form. */
  loadProfile(file: ScenarioFile, id = 'spec-profile'): SavedProfile {
    const profile = asSavedProfile(file, id);
    if (this.pageFixture) {
      this.pageComponent.onProfileLoadRequested(profile);
    } else {
      this.formComponent.loadProfile(profile);
    }
    return profile;
  }

  tick(ms: number): void {
    tick(ms);
    this.detectChanges();
  }

  /**
   * Advance past every known timer, then drain what is left.
   *
   * Prefer this over hand-picked ticks in any spec that is not deliberately probing one instant —
   * `AppDataService` resolves some subscribers through a promise, and a spec that only ticks a
   * chosen number of milliseconds can observe an interleaving the browser never produces.
   */
  settle(): void {
    tick(SETTLE_MS);
    flush();
    this.detectChanges();
  }

  /**
   * Runs change detection WITHOUT Angular's `checkNoChanges` verification pass.
   *
   * That pass is skipped deliberately, and it is a finding rather than a convenience: mid-restore
   * the tree genuinely does throw `NG0100 ExpressionChangedAfterItHasBeenCheckedError` on
   * `vesselTypeName`, because `onVesselEngineConfigSelected` assigns it from inside a subscription
   * that runs during change detection. That is the same uncoordinated-write problem this epic
   * exists to remove — see design §7.8. Letting it abort every spec would hide the assertions we
   * are actually here to make; C-E is where it gets fixed, and this call reverts to the checked
   * form then.
   */
  detectChanges(): void {
    this.pageFixture?.detectChanges(false);
    this.formFixture?.detectChanges(false);
  }

  /** The checked pass, for a spec that deliberately wants to observe NG0100. */
  detectChangesChecked(): void {
    this.pageFixture?.detectChanges(true);
    this.formFixture?.detectChanges(true);
  }

  /**
   * Destroy the tree and drain what the client leaves behind.
   *
   * MUST be called inside the `fakeAsync` body, otherwise the zone reports "N timer(s) still in
   * the queue" and buries the real assertion. Two kinds of leftovers exist:
   *  - the 30 s auto-draft `setInterval` (periodic — cleared by `ngOnDestroy`, discarded here as a
   *    belt-and-braces measure);
   *  - un-cleared `setTimeout`s, notably the 800 ms profile-apply fallback
   *    (`vessel-input-form.component.ts:538`), which has no handle and no cancellation path.
   *    `flush()` after destroy runs them out; their guards make them no-ops on a dead component.
   */
  dispose(): void {
    if (this.disposed) {
      return;
    }
    this.disposed = true;
    this.pageFixture?.destroy();
    this.formFixture?.destroy();
    flush();
    discardPeriodicTasks();
    RestoreHarness.clearStorage();
  }
}
