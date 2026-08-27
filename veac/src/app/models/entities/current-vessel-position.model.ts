export interface CurrentPosition {
    latitude: number;
    longitude: number;
    heading?: number;
    course?: number;
    status?: string;
    vesselName?: string;
    positionUpdatedAt?: Date;
}