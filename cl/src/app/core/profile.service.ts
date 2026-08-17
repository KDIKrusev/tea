import { Injectable } from '@angular/core';
import { SavedProfile, PROFILE_SCHEMA_VERSION } from './profile.types';
import { CalculatorInput } from '../calculations/calculator.types';

const PROFILES_KEY = 'ksailcalc_profiles';
const DRAFT_KEY = 'ksailcalc_draft';

@Injectable({ providedIn: 'root' })
export class ProfileService {

  // ─── CRUD ────────────────────────────────────────────────────────────────────

  getAll(): SavedProfile[] {
    try {
      const raw = localStorage.getItem(PROFILES_KEY);
      if (!raw) {return [];}
      const parsed: SavedProfile[] = JSON.parse(raw);
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  save(
    name: string,
    input: CalculatorInput,
    vesselTypeName: string,
    vesselCategory: string,
    vesselSize: number,
    vesselSpeed: number
  ): SavedProfile {
    const profiles = this.getAll();
    const now = new Date().toISOString();

    const profile: SavedProfile = {
      id: this.generateId(),
      name: name.trim(),
      createdAt: now,
      updatedAt: now,
      vesselTypeName,
      vesselCategory,
      vesselSize,
      vesselSpeed,
      input,
      version: PROFILE_SCHEMA_VERSION
    };

    profiles.push(profile);
    this.persist(profiles);
    return profile;
  }

  delete(id: string): void {
    const profiles = this.getAll().filter(p => p.id !== id);
    this.persist(profiles);
  }

  rename(id: string, newName: string): void {
    const profiles = this.getAll().map(p =>
      p.id === id
        ? { ...p, name: newName.trim(), updatedAt: new Date().toISOString() }
        : p
    );
    this.persist(profiles);
  }

  // ─── AUTO-DRAFT ──────────────────────────────────────────────────────────────

  saveDraft(
    input: CalculatorInput,
    vesselTypeName: string,
    vesselCategory: string,
    vesselSize: number,
    vesselSpeed: number
  ): void {
    try {
      const draft = { input, vesselTypeName, vesselCategory, vesselSize, vesselSpeed, savedAt: new Date().toISOString() };
      localStorage.setItem(DRAFT_KEY, JSON.stringify(draft));
    } catch {
      // Ignore storage quota errors silently
    }
  }

  loadDraft(): { input: CalculatorInput; vesselTypeName: string; vesselCategory: string; vesselSize: number; vesselSpeed: number } | null {
    try {
      const raw = localStorage.getItem(DRAFT_KEY);
      if (!raw) {return null;}
      const parsed = JSON.parse(raw) as {
        input?: CalculatorInput;
        vesselTypeName?: string;
        vesselCategory?: string;
        vesselSize?: number;
        vesselSpeed?: number;
      };
      if (
        !parsed ||
        typeof parsed.vesselCategory !== 'string' ||
        typeof parsed.vesselSize !== 'number' ||
        !Number.isFinite(parsed.vesselSize) ||
        typeof parsed.vesselSpeed !== 'number' ||
        !Number.isFinite(parsed.vesselSpeed) ||
        !this.isValidCalculatorInput(parsed.input)
      ) {
        // Pre-parametric drafts (vesselTypeName only) are not restorable anymore
        if (parsed && typeof parsed.vesselTypeName === 'string' && typeof parsed.vesselCategory !== 'string') {
          console.warn('Discarding legacy draft (pre-parametric vessel selection format).');
          this.clearDraft();
        }
        return null;
      }
      return {
        input: parsed.input,
        vesselTypeName: parsed.vesselTypeName ?? '',
        vesselCategory: parsed.vesselCategory,
        vesselSize: parsed.vesselSize,
        vesselSpeed: parsed.vesselSpeed
      };
    } catch {
      return null;
    }
  }

  clearDraft(): void {
    localStorage.removeItem(DRAFT_KEY);
  }

  // ─── EXPORT / IMPORT ─────────────────────────────────────────────────────────

  exportToJson(profile: SavedProfile): void {
    const json = JSON.stringify(profile, null, 2);
    const blob = new Blob([json], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    const safeName = profile.name.replace(/[^a-z0-9_-]/gi, '_').toLowerCase();
    link.href = url;
    link.download = `ksailcalc_${safeName}.json`;
    link.click();
    URL.revokeObjectURL(url);
  }

  importFromJson(file: File): Promise<SavedProfile> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = (event): void => {
        try {
          const parsed = JSON.parse(event.target?.result as string) as unknown;
          if (!this.isValidProfile(parsed)) {
            reject(new Error('Invalid profile file: missing required fields.'));
            return;
          }
          const importedProfile = parsed as SavedProfile;
          const now = new Date().toISOString();
          // Re-assign a new ID and timestamps to avoid collisions
          const imported: SavedProfile = {
            ...importedProfile,
            id: this.generateId(),
            createdAt: this.isValidIsoDate(importedProfile.createdAt) ? importedProfile.createdAt : now,
            updatedAt: now,
            version: Number.isFinite(importedProfile.version) ? importedProfile.version : PROFILE_SCHEMA_VERSION
          };
          const profiles = this.getAll();
          profiles.push(imported);
          this.persist(profiles);
          resolve(imported);
        } catch {
          reject(new Error('Failed to parse profile file.'));
        }
      };
      reader.onerror = (): void => reject(new Error('Failed to read file.'));
      reader.readAsText(file);
    });
  }

  // ─── PRIVATE HELPERS ─────────────────────────────────────────────────────────

  private persist(profiles: SavedProfile[]): void {
    try {
      localStorage.setItem(PROFILES_KEY, JSON.stringify(profiles));
    } catch {
      throw new Error('Unable to persist profiles in local storage.');
    }
  }

  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).slice(2, 9)}`;
  }

  private isValidProfile(obj: unknown): obj is SavedProfile {
    if (typeof obj !== 'object' || obj === null) {return false;}
    const p = obj as Record<string, unknown>;
    return (
      typeof p['name'] === 'string' &&
      typeof p['vesselTypeName'] === 'string' &&
      typeof p['vesselCategory'] === 'string' &&
      typeof p['vesselSize'] === 'number' &&
      Number.isFinite(p['vesselSize']) &&
      typeof p['vesselSpeed'] === 'number' &&
      Number.isFinite(p['vesselSpeed']) &&
      this.isValidCalculatorInput(p['input'])
    );
  }

  private isValidCalculatorInput(input: unknown): input is CalculatorInput {
    if (typeof input !== 'object' || input === null) {return false;}
    const i = input as Record<string, unknown>;

    const requiredNumbers = [
      'propulsionPower', 'hotelLoad', 'seaMargin', 'meCapacityPerEngine', 'meCount',
      'sgCapacityPerEngine', 'aeCapacityPerEngine', 'aeCount', 'mainEngineTypeId',
      'auxEngineTypeId', 'batteryCapacity', 'fuelPrice', 'vesselSpeedKnots'
    ];

    for (const key of requiredNumbers) {
      if (typeof i[key] !== 'number' || !Number.isFinite(i[key] as number)) {
        return false;
      }
    }

    if (typeof i['sailInstalled'] !== 'boolean') {return false;}

    const optionalNumbers = [
      'transitHours', 'transitHotelPowerKW', 'dpHours', 'dpHotelPowerKW', 'requiredDPPowerKW',
      'portHotelPowerKW', 'portHours', 'anchorHotelPowerKW', 'anchorHours',
      'maneuveringPropulsionPowerKW', 'maneuveringHotelPowerKW', 'maneuveringHours',
      'hotelLoadVariationKw', 'trueWindSpeed', 'windAngleRelVessel', 'baselineIndex',
      'maxPtiPerEngineKw', 'dpRedundancyRequirementKw', 'missionHeavyConsumerMaxKw',
      'othersConsumerMaxKw'
    ];

    for (const key of optionalNumbers) {
      const value = i[key];
      if (value !== undefined && (typeof value !== 'number' || !Number.isFinite(value))) {
        return false;
      }
    }

    if (i['dpWeatherCondition'] !== undefined) {
      const weather = i['dpWeatherCondition'];
      if (weather !== 'Calm' && weather !== 'Moderate' && weather !== 'Rough') {
        return false;
      }
    }

    if (i['sailEnabled'] !== undefined && typeof i['sailEnabled'] !== 'boolean') {
      return false;
    }

    if (i['dpEnabled'] !== undefined && typeof i['dpEnabled'] !== 'boolean') {
      return false;
    }

    if (i['vesselTypeName'] !== undefined && typeof i['vesselTypeName'] !== 'string') {
      return false;
    }

    // v3: optional battery configuration (absent in v2 profiles ⇒ battery disabled)
    if (i['battery'] !== undefined && i['battery'] !== null && !this.isValidBattery(i['battery'])) {
      return false;
    }

    return true;
  }

  private isValidBattery(battery: unknown): boolean {
    if (typeof battery !== 'object' || battery === null) {return false;}
    const b = battery as Record<string, unknown>;
    if (typeof b['capacityKwh'] !== 'number' || !Number.isFinite(b['capacityKwh'])) {return false;}
    if (typeof b['powerKw'] !== 'number' || !Number.isFinite(b['powerKw'])) {return false;}
    if (!Array.isArray(b['relevantModes'])) {return false;}
    return (b['relevantModes'] as unknown[]).every(m => m === 'Transit' || m === 'DP' || m === 'Port');
  }

  private isValidIsoDate(value: string): boolean {
    return typeof value === 'string' && !Number.isNaN(Date.parse(value));
  }
}
