import { Component, input, output } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Category } from '../../core/models/dashboard.model';

@Component({
  selector: 'app-sidebar',
  standalone: false,
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
  host: {
    class: 'flex flex-col flex-shrink-0 h-full z-25'
  }
})
export class SidebarComponent {
  public categories = input<Category[]>([]);
  public activeTabId = input<string>('');
  public selectTab = output<string>();

  constructor(private http: HttpClient) {}

  public onTabClick(id: string): void {
    this.selectTab.emit(id);
  }

  public async restartExplorer(): Promise<void> {
    try {
      await this.http.post('/api/system/restart-explorer', {}).toPromise();
    } catch (e) {
      console.warn('Erreur lors du redémarrage de l’Explorer :', e);
    }
  }
}
