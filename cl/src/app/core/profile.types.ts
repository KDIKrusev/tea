import { CalculatorInput } from '../calculations/calculator.types';

// v2 (Epic 1): vessel selection is parametric — category + size + speed.
// vesselTypeName remains as a human-readable label only (e.g. "Bulk Carrier 75,000 dwt").
// v3 (Battery Increment D): input may carry an optional `battery` object
// ({ capacityKwh, powerKw, relevantModes }). v2 profiles load unchanged — absence of
// `battery` simply means "battery disabled" (no explicit migration step required).
export const PROFILE_SCHEMA_VERSION = 3;

export interface SavedProfile {
  id: string;
  name: string;
  createdAt: string;       // ISO 8601
  updatedAt: string;       // ISO 8601
  vesselTypeName: string;  // display label, composed from category + size + unit
  vesselCategory: string;
  vesselSize: number;
  vesselSpeed: number;
  input: CalculatorInput;
  version: number;         // for future schema migrations
}
