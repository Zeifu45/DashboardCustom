import { Component, OnInit, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Category, LinkItem, DashboardData } from './core/models/dashboard.model';

@Component({
  selector: 'app-root',
  standalone: false,
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  public categories = signal<Category[]>([]);
  public links = signal<LinkItem[]>([]);
  public activeTabId = signal<string>('barre');
  public isConfigOpen = signal<boolean>(false);

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadData();
  }

  private loadData(): void {
    this.http.get<DashboardData>('/api/data').subscribe({
      next: (data) => {
        if (data) {
          this.categories.set(data.categories || []);
          this.links.set(data.links || []);
          if (data.categories && data.categories.length > 0) {
            this.activeTabId.set(data.categories[0].id);
          }
        }
      },
      error: (err) => console.error('Erreur lors du chargement des données du dashboard :', err)
    });
  }

  public changeTab(tabId: string): void {
    this.activeTabId.set(tabId);
  }

  public openConfig(): void {
    console.log('Ouverture de la modale de configuration demandée');
    this.isConfigOpen.set(true);
  }
}
