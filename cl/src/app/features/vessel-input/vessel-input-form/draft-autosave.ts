import { ProfileService } from '../../../core/profile.service';
import { CalculatorInput } from '../../../calculations/calculator.types';
import { PROFILE_SCHEMA_VERSION, SavedProfile } from '../../../core/profile.types';

/** Everything one auto-draft needs; null means "not enough state yet, skip this tick". */
export interface DraftSnapshot {
  input: CalculatorInput;
  vesselTypeName: string;
  vesselCategory: string;
  vesselSize: number;
  vesselSpeed: number;
}

const DRAFT_INTERVAL_MS = 30_000;

/**
 * Periodically saves whatever the form currently holds, so a hard refresh does not lose it.
 *
 * Extracted from `VesselInputFormComponent` (story C-G): a timer that writes to localStorage has
 * nothing to do with the load sequence it was sitting next to, and having both in one file made
 * "which timer is this?" a real question during C-E.
 *
 * **A plain class, not an `@Injectable`.** It owns a running interval and belongs to exactly one
 * component, which starts and stops it. Making it injectable would force a choice between
 * `providedIn: 'root'` — one shared timer outliving the form it saves — and a component-level
 * provider that only exists to satisfy the decorator. Neither buys anything: its single dependency
 * is passed in, and the component already has it.
 *
 * The caller supplies a snapshot function that returns `null` whenever the form is not in a state
 * worth persisting; this class never inspects the form itself.
 */
export class DraftAutosave {
  private timer: ReturnType<typeof setInterval> | null = null;

  constructor(private readonly profileService: ProfileService) {}

  start(snapshot: () => DraftSnapshot | null): void {
    this.stop();
    this.timer = setInterval(() => {
      const draft = snapshot();
      if (!draft) {
        return;
      }
      this.profileService.saveDraft(
        draft.input,
        draft.vesselTypeName,
        draft.vesselCategory,
        draft.vesselSize,
        draft.vesselSpeed
      );
    }, DRAFT_INTERVAL_MS);
  }

  stop(): void {
    if (this.timer !== null) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  /**
   * The saved draft as a restorable profile, or null when there is nothing usable stored.
   *
   * Saving and restoring a draft are the same concern, so they live together. The timestamps are
   * synthetic — a draft has no history worth keeping, only a current state — and `version` is the
   * current schema: a v2 draft (no battery object) restores unchanged, absence simply meaning
   * "battery disabled".
   */
  loadRestorable(): SavedProfile | null {
    const draft = this.profileService.loadDraft();
    if (!draft || !draft.input || !draft.vesselCategory
      || !Number.isFinite(draft.vesselSize) || !Number.isFinite(draft.vesselSpeed)) {
      return null;
    }

    const now = new Date().toISOString();
    return {
      id: 'draft',
      name: 'Auto Draft',
      createdAt: now,
      updatedAt: now,
      vesselTypeName: draft.vesselTypeName,
      vesselCategory: draft.vesselCategory,
      vesselSize: draft.vesselSize,
      vesselSpeed: draft.vesselSpeed,
      input: draft.input,
      version: PROFILE_SCHEMA_VERSION
    };
  }
}
