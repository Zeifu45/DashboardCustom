import { Component, Input, OnInit, OnChanges, SimpleChanges, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface LinkItem {
  id: string;
  tabId: string;
  name: string;
  url: string;
  iconFileName?: string;
}

interface AppItem {
  id: string;
  tabId: string;
  name: string;
  path: string;
  iconFileName?: string;
}

interface CategoryData {
  categories: any[];
  links: LinkItem[];
  apps: AppItem[];
}

@Component({
  selector: 'app-data-view',
  standalone: false,
  templateUrl: './data-view.component.html',
  styleUrl: './data-view.component.css'
})
export class DataViewComponent implements OnInit, OnChanges {
  @Input() public activeTabId: string = '';

  public links = signal<LinkItem[]>([]);
  public apps = signal<AppItem[]>([]);
  public filteredLinks = signal<LinkItem[]>([]);
  public filteredApps = signal<AppItem[]>([]);

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['activeTabId']) {
      this.filterData(this.activeTabId);
    }
  }

  public loadData(): void {
    this.http.get<CategoryData>('/api/data').subscribe({
      next: (res) => {
        if (res) {
          this.links.set(res.links || []);
          this.apps.set(res.apps || []);
          this.filterData(this.activeTabId);
        }
      },
      error: (err) => console.warn(err)
    });
  }

  private filterData(tabId: string): void {
    if (!tabId) {
      this.filteredLinks.set(this.links());
      this.filteredApps.set(this.apps());
      return;
    }
    this.filteredLinks.set(this.links().filter(l => l.tabId === tabId));
    this.filteredApps.set(this.apps().filter(a => a.tabId === tabId));
  }

  public getIconUrl(fileName?: string): string {
    if (!fileName || fileName === 'default.png') {
      return '';
    }
    return `/api/public/images/${fileName}`;
  }

  public launchApp(app: AppItem): void {
    this.http.post(`/api/system/launch?key=${encodeURIComponent(app.id)}`, {}).subscribe({
      error: (err) => console.warn('Erreur lancement appli:', err)
    });
  }
}
