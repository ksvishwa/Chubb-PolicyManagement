import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BulkFlagRequest,
  PagedResult,
  Policy,
  PolicyFilterParams,
  PolicySummary
} from '../models/policy.model';

@Injectable({ providedIn: 'root' })
export class PolicyApiService {
  private readonly baseUrl = '/api/v1/policies';

  constructor(private readonly http: HttpClient) {}

  getPolicies(filters: PolicyFilterParams): Observable<PagedResult<Policy>> {
    let params = new HttpParams();
    if (filters.page != null) params = params.set('page', filters.page);
    if (filters.size != null) params = params.set('size', filters.size);
    if (filters.sort) params = params.set('sort', filters.sort);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.lineOfBusiness) params = params.set('lineOfBusiness', filters.lineOfBusiness);
    if (filters.region) params = params.set('region', filters.region);
    if (filters.effectiveDateFrom) params = params.set('effectiveDateFrom', filters.effectiveDateFrom);
    if (filters.effectiveDateTo) params = params.set('effectiveDateTo', filters.effectiveDateTo);
    if (filters.search) params = params.set('search', filters.search);
    return this.http.get<PagedResult<Policy>>(this.baseUrl, { params });
  }

  getPolicyById(id: string): Observable<Policy> {
    return this.http.get<Policy>(`${this.baseUrl}/${id}`);
  }

  bulkFlagPolicies(request: BulkFlagRequest): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/flag`, request);
  }

  getSummary(): Observable<PolicySummary> {
    return this.http.get<PolicySummary>(`${this.baseUrl}/summary`);
  }
}
