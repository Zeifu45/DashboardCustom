import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Category, DashboardData } from '../models/dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  public categories = signal<Category[]>([]);
  public activeCategoryId = signal<string | null>(null);

  constructor(private http: HttpClient) {}

  loadDashboardData() {
    this.http.get<DashboardData>('/api/data').subscribe({
      next: (data) => {
        const sorted = data.categories.sort((a, b) => a.order - b.order);
        this.categories.set(sorted);
        if (sorted.length > 0 && !this.activeCategoryId()) {
          this.activeCategoryId.set(sorted[0].id);
        }
      },
      error: (err) => console.error('Erreur chargement dashboard:', err)
    });
  }

  selectCategory(id: string) {
    this.activeCategoryId.set(id);
  }
}
