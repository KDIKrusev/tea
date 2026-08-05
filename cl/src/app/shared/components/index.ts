// Shared, reusable presentation components.
//
// Kept deliberately small: a component earns a place here by being used in more than one feature.
// Three former entries (result-metric-card, form-select-field, savings-breakdown-card) were removed
// in story C-D — they were exported, compiled and shipped in the bundle without a single template
// ever rendering them.

export * from './form-input-field/form-input-field.component';

// Shared services
export * from '../services';
