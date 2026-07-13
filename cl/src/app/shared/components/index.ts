// Maritime Component Library Foundation
// This file exports shared components for the Energy Savings Calculator

// Component library structure (to be implemented in future stories):
// - Layout Components: headers, containers, grids  
// - Maritime-specific: vessel selectors, calculation displays
// - Charts: Chart.js integration components
// - Forms: inputs, selects, validation components

// Reusable form components
export * from './form-input-field/form-input-field.component';
export * from './form-select-field/form-select-field.component';

// Reusable result metric card (handles all metric types)
export * from './result-metric-card/result-metric-card.component';

// Shared services
export * from '../services';
