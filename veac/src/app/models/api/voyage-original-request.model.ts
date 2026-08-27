export interface VoyageOriginalRequest {
  etdEtaVisibility: string;
  etd: Date;
  etdTimeWindowText: string;
  eta: Date;
  etaTimeWindowText: string;
  speed?: {
    min: number;
    max: number;
  };
  speedMin?: number;
  speedMax?: number;
  routeName: string;
  etdTimeWindow: number;
  etaTimeWindow: number;
}
