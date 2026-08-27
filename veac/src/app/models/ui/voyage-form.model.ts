export interface VoyageFormData {
    vessel: {
      name: string;
      id: number | null;
    };
    route: string;
    timeWindowMode: 'etd' | 'eta';

    speed: {
      min: number;
      max: number;
    };
  }