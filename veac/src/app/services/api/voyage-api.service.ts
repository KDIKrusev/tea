import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { firstValueFrom, Subject } from 'rxjs';
import { voyageEnergyAdvisorRequest } from '../../models/api/voyage-energy-advisor-request.model';
import { voyageEnergyAdvisorResponse } from '../../models/api/voyage-energy-advisor-response.model';
import {VoyageEnergyAdvisorLiveRequest} from '../../models/api/voyage-energy-advisor-live-request.model'
import {VoyageEnergyAdvisorLiveResponse} from '../../models/api/voyage-energy-advisor-live-response.model'
import {VoyageCalculationConfigurationRequest} from '../../models/api/voyage-calculation-configuration-request.model';
import {VoyageCalculationConfigurationResponse} from '../../models/api/voyage-calculation-configuration-response.model';
import { Route } from '../../models/entities/route.model';
import { Vessel } from '../../models/entities/vessel.model';
import { ConfigService } from './config.service';
import { takeUntil } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class VoyageApiService {

  constructor(
    private httpClient: HttpClient,
    private configService: ConfigService
  ) {}

  getVessels(): Promise<Vessel[]> {
    const url = `${this.configService.getApiBaseUrl()}/api/v1/vessel/user-vessels`;
    return firstValueFrom(this.httpClient.get<Vessel[]>(url));
  }

  getRoutes(): Promise<string[]> {
    const url = `${this.configService.getApiBaseUrl()}/api/v1/route`;
    return firstValueFrom(this.httpClient.get<string[]>(url));
  }

  getRouteDetails(routeName: string): Promise<Route> {
    const url = `${this.configService.getApiBaseUrl()}/api/v1/route/routedetails/${encodeURIComponent(routeName)}`;
    return firstValueFrom(this.httpClient.get<Route>(url));
  }

  sendCalculationRequest(
    requestBody: voyageEnergyAdvisorRequest,
    cancelToken$: Subject<void>
  ): Promise<voyageEnergyAdvisorResponse> {
    const url = `${this.configService.getApiBaseUrl()}/api/v1/voyageEnergyAdvisor/update`;
    return firstValueFrom(
      this.httpClient.post<voyageEnergyAdvisorResponse>(url, requestBody)
        .pipe(takeUntil(cancelToken$))
    );
  }

   getLiveVoyageData(request: VoyageEnergyAdvisorLiveRequest): Promise<VoyageEnergyAdvisorLiveResponse> {
    const url = `${this.configService.getApiBaseUrl()}/api/v1/voyageEnergyAdvisor/live`;
    return firstValueFrom(this.httpClient.post<VoyageEnergyAdvisorLiveResponse>(url, request));
  }

   getVoyageCalculationConfiguration(): Promise<VoyageCalculationConfigurationRequest> {
    const url = `${this.configService.getApiBaseUrl()}/api/v1/configuration/calculation-configuration`;
    return firstValueFrom(this.httpClient.get<VoyageCalculationConfigurationRequest>(url));
  }

  updateVoyageCalculationConfiguration(
    request: VoyageCalculationConfigurationRequest
  ): Promise<VoyageCalculationConfigurationResponse> {
    const url = `${this.configService.getApiBaseUrl()}/api/v1/configuration/calculation-configuration`;
    return firstValueFrom(this.httpClient.put<VoyageCalculationConfigurationResponse>(url, request));
  }

}
