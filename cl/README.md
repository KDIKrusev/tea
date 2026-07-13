# K-Sail Calculator - Maritime iEMS Calculator

An Angular 18+ application for maritime fuel consumption and ROI calculations, migrated from HTML/JavaScript to provide enhanced functionality for maritime professionals.

## Project Overview

This is a professional maritime calculator designed for naval architects, marine engineers, and fleet managers to calculate fuel consumption savings and return on investment for various maritime technologies.

## Technology Stack

- **Angular**: 18.2+ with TypeScript 5.5+
- **Node.js**: 22.9+ with npm 10.8+
- **TypeScript**: Strict mode enabled for calculation accuracy
- **Target Browsers**: Chrome 90+, Firefox 88+, Safari 14+, Edge 90+

## Project Structure

```
src/app/
├── core/                    # Singleton services, guards
├── shared/                  # Shared components, pipes, directives
├── features/
│   ├── vessel-input/        # Vessel data input feature
│   ├── calculation/         # Calculation engine and services
│   ├── results-display/     # Results visualization feature
│   └── charts/              # Chart components and services
├── models/                  # TypeScript interfaces and types
└── utils/                   # Utility functions
```

## Development Setup

### Prerequisites
- Node.js 18+ and npm 9+
- Angular CLI 17+

### Installation & Development Server

```bash
# Navigate to project directory
cd k-sail-calculator

# Install dependencies (if needed)
npm install

# Start development server
npm start
```

Navigate to `http://localhost:4200/`. The application will automatically reload if you change any of the source files.

## Build & Deployment

```bash
# Development build
npm run build

# Production build
npm run build -- --configuration production
```

The build artifacts are stored in the `dist/k-sail-calculator/` directory, optimized for Vercel deployment.

## Environment Configuration

- **Development**: `src/environments/environment.ts`
- **Production**: `src/environments/environment.prod.ts`

## Testing

```bash
# Unit tests via Karma
npm test

# End-to-end tests (will be added in Story 1.4)
# npm run e2e
```

## Running end-to-end tests

Run `ng e2e` to execute the end-to-end tests via a platform of your choice. To use this command, you need to first add a package that implements end-to-end testing capabilities.

## Further help

To get more help on the Angular CLI use `ng help` or go check out the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
