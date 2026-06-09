import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PolicyApiService } from '../../core/services/policy-api.service';
import { PolicySummary } from '../../core/models/policy.model';

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="summary-panel" aria-label="Policy statistics">
      <h2>Summary</h2>
      <div aria-live="polite">
        @if (loading()) {
          <p>Loading summary...</p>
        } @else if (error()) {
          <p class="error" role="alert">{{ error() }}</p>
        } @else if (summary()) {
          <dl class="summary-stats">
            <div class="stat">
              <dt>Total Policies</dt>
              <dd>{{ summary()!.totalPolicies }}</dd>
            </div>
            <div class="stat">
              <dt>Active</dt>
              <dd>{{ summary()!.activePolicies }}</dd>
            </div>
            <div class="stat">
              <dt>Expired</dt>
              <dd>{{ summary()!.expiredPolicies }}</dd>
            </div>
            <div class="stat">
              <dt>Pending</dt>
              <dd>{{ summary()!.pendingPolicies }}</dd>
            </div>
            <div class="stat">
              <dt>Cancelled</dt>
              <dd>{{ summary()!.cancelledPolicies }}</dd>
            </div>
            <div class="stat">
              <dt>Flagged for Review</dt>
              <dd>{{ summary()!.flaggedForReview }}</dd>
            </div>
            <div class="stat">
              <dt>Total Premium</dt>
              <dd>{{ summary()!.totalPremiumAmount | number:'1.2-2' }}</dd>
            </div>
          </dl>
        }
      </div>
    </section>
  `,
})
export class SummaryComponent implements OnInit {
  loading = signal(false);
  error = signal<string | null>(null);
  summary = signal<PolicySummary | null>(null);

  constructor(private readonly apiService: PolicyApiService) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.apiService.getSummary().subscribe({
      next: data => { this.summary.set(data); this.loading.set(false); },
      error: err => { this.error.set(err.message); this.loading.set(false); }
    });
  }
}
