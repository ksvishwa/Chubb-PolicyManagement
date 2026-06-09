import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { PolicyApiService } from '../../../core/services/policy-api.service';
import { Policy, PagedResult, PolicyFilterParams } from '../../../core/models/policy.model';

@Component({
  selector: 'app-policy-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="policy-list-container" role="main">
      <h1>Policies</h1>

      <!-- Filters -->
      <section class="filters" aria-label="Policy filters">
        <input
          type="search"
          [(ngModel)]="searchTerm"
          (ngModelChange)="onSearchChange()"
          placeholder="Search by policy number, holder, or underwriter"
          aria-label="Search policies"
          class="search-input"
        />
        <select [(ngModel)]="selectedStatus" (ngModelChange)="onFilterChange()" aria-label="Filter by status">
          <option value="">All Statuses</option>
          <option value="Active">Active</option>
          <option value="Expired">Expired</option>
          <option value="Pending">Pending</option>
          <option value="Cancelled">Cancelled</option>
        </select>
        <select [(ngModel)]="selectedLob" (ngModelChange)="onFilterChange()" aria-label="Filter by line of business">
          <option value="">All Lines of Business</option>
          <option value="Property">Property</option>
          <option value="Casualty">Casualty</option>
          <option value="AAndH">A&amp;H</option>
          <option value="Marine">Marine</option>
        </select>
        <button (click)="flagSelected()" [disabled]="selectedIds().length === 0" aria-label="Flag selected policies for review">
          Flag Selected ({{ selectedIds().length }})
        </button>
      </section>

      <!-- Loading / Error / Empty states -->
      <div aria-live="polite" aria-atomic="true">
        @if (loading()) {
          <p class="state-message">Loading policies...</p>
        } @else if (error()) {
          <p class="state-message error" role="alert">{{ error() }}</p>
        } @else if (result()?.data?.length === 0) {
          <p class="state-message">No policies found.</p>
        }
      </div>

      <!-- Table -->
      @if (!loading() && result()?.data?.length) {
        <table class="policy-table" aria-label="Policy list">
          <thead>
            <tr>
              <th scope="col">
                <input type="checkbox" (change)="toggleSelectAll($event)" aria-label="Select all policies" />
              </th>
              <th scope="col" (click)="sortBy('policyNumber')" tabindex="0" role="columnheader" aria-sort="none">Policy #</th>
              <th scope="col">Policyholder</th>
              <th scope="col">Line of Business</th>
              <th scope="col" (click)="sortBy('status')" tabindex="0" role="columnheader">Status</th>
              <th scope="col" (click)="sortBy('premiumAmount')" tabindex="0" role="columnheader">Premium</th>
              <th scope="col">Region</th>
              <th scope="col" (click)="sortBy('effectiveDate')" tabindex="0" role="columnheader">Effective</th>
              <th scope="col" (click)="sortBy('expiryDate')" tabindex="0" role="columnheader">Expiry</th>
              <th scope="col">Flagged</th>
            </tr>
          </thead>
          <tbody>
            @for (policy of result()!.data; track policy.id) {
              <tr [class.flagged]="policy.flaggedForReview">
                <td>
                  <input
                    type="checkbox"
                    [checked]="isSelected(policy.id)"
                    (change)="toggleSelect(policy.id)"
                    [attr.aria-label]="'Select policy ' + policy.policyNumber"
                  />
                </td>
                <td>{{ policy.policyNumber }}</td>
                <td>{{ policy.policyholderName }}</td>
                <td>{{ policy.lineOfBusiness }}</td>
                <td><span class="status-badge" [attr.data-status]="policy.status.toLowerCase()">{{ policy.status }}</span></td>
                <td>{{ policy.premiumAmount | number:'1.2-2' }} {{ policy.currency }}</td>
                <td>{{ policy.region }}</td>
                <td>{{ policy.effectiveDate }}</td>
                <td>{{ policy.expiryDate }}</td>
                <td>{{ policy.flaggedForReview ? 'Yes' : 'No' }}</td>
              </tr>
            }
          </tbody>
        </table>

        <!-- Pagination -->
        <nav aria-label="Pagination" class="pagination">
          <button (click)="prevPage()" [disabled]="currentPage() <= 1" aria-label="Previous page">Previous</button>
          <span>Page {{ currentPage() }} of {{ result()!.totalPages }}</span>
          <button (click)="nextPage()" [disabled]="currentPage() >= result()!.totalPages" aria-label="Next page">Next</button>
        </nav>
      }
    </div>
  `,
})
export class PolicyListComponent implements OnInit {
  loading = signal(false);
  error = signal<string | null>(null);
  result = signal<PagedResult<Policy> | null>(null);
  selectedIds = signal<string[]>([]);
  currentPage = signal(1);
  pageSize = signal(20);
  currentSort = signal('createdAt,desc');
  searchTerm = '';
  selectedStatus = '';
  selectedLob = '';

  constructor(
    private readonly apiService: PolicyApiService,
    private readonly router: Router,
    private readonly route: ActivatedRoute
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      this.currentPage.set(+(params['page'] ?? 1));
      this.currentSort.set(params['sort'] ?? 'createdAt,desc');
      this.selectedStatus = params['status'] ?? '';
      this.selectedLob = params['lineOfBusiness'] ?? '';
      this.searchTerm = params['search'] ?? '';
      this.loadPolicies();
    });
  }

  loadPolicies(): void {
    this.loading.set(true);
    this.error.set(null);
    const filters: PolicyFilterParams = {
      page: this.currentPage(),
      size: this.pageSize(),
      sort: this.currentSort(),
      status: this.selectedStatus || undefined,
      lineOfBusiness: this.selectedLob || undefined,
      search: this.searchTerm || undefined,
    };

    this.apiService.getPolicies(filters).subscribe({
      next: data => { this.result.set(data); this.loading.set(false); },
      error: err => { this.error.set(err.message); this.loading.set(false); }
    });
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.updateUrl();
  }

  onSearchChange(): void {
    this.currentPage.set(1);
    this.updateUrl();
  }

  sortBy(field: string): void {
    const [currentField, currentDir] = this.currentSort().split(',');
    const dir = currentField === field && currentDir === 'asc' ? 'desc' : 'asc';
    this.currentSort.set(`${field},${dir}`);
    this.updateUrl();
  }

  prevPage(): void {
    if (this.currentPage() > 1) { this.currentPage.update(p => p - 1); this.updateUrl(); }
  }

  nextPage(): void {
    const total = this.result()?.totalPages ?? 1;
    if (this.currentPage() < total) { this.currentPage.update(p => p + 1); this.updateUrl(); }
  }

  toggleSelect(id: string): void {
    this.selectedIds.update(ids =>
      ids.includes(id) ? ids.filter(i => i !== id) : [...ids, id]);
  }

  isSelected(id: string): boolean {
    return this.selectedIds().includes(id);
  }

  toggleSelectAll(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    const allIds = this.result()?.data.map(p => p.id) ?? [];
    this.selectedIds.set(checked ? allIds : []);
  }

  flagSelected(): void {
    const ids = this.selectedIds();
    if (!ids.length) return;
    this.apiService.bulkFlagPolicies({ policyIds: ids }).subscribe({
      next: () => { this.selectedIds.set([]); this.loadPolicies(); },
      error: err => this.error.set(err.message)
    });
  }

  private updateUrl(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        page: this.currentPage(),
        sort: this.currentSort(),
        status: this.selectedStatus || null,
        lineOfBusiness: this.selectedLob || null,
        search: this.searchTerm || null,
      },
      queryParamsHandling: 'merge'
    });
  }
}
