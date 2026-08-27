import { Waypoint } from './waypoint.model';

export interface Route {
    routeName: string;
    waypoints: Waypoint[];
}
