import { Component, OnInit, signal, Output, EventEmitter } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface Category {
  id: string;
  name: string;
  order: number;
  icon: string;
}

interface LinkItem {
  id?: string;
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

@Component({
  selector: 'app-config',
  standalone: false,
  templateUrl: './config.component.html',
  styleUrl: './config.component.css'
})
export class ConfigComponent implements OnInit {
  @Output() public onClose = new EventEmitter<void>();
  @Output() public onDataChanged = new EventEmitter<void>();

  public activeTab = signal<'categories' | 'links' | 'apps'>('categories');
  
  public categories = signal<Category[]>([]);
  public links = signal<LinkItem[]>([]);
  public apps = signal<AppItem[]>([]);

  // Formulaires / États d'édition
  public editingCategoryId: string | null = null;
  public newCategory: Category = { id: '', name: '', order: 0, icon: 'fa-star' };

  public editingLinkId: string | null = null;
  public newLink: LinkItem = { tabId: '', name: '', url: '' };

  public editingAppId: string | null = null;
  public newApp: AppItem = { id: '', tabId: '', name: '', path: '' };

  public selectedLinkFile: File | null = null;
  public linkPreviewUrl: string | null = null;

  public selectedAppFile: File | null = null;
  public appPreviewUrl: string | null = null;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadData();
  }

  public loadData(): void {
    this.http.get<any>('/api/data').subscribe({
      next: (res) => {
        if (res) {
          this.categories.set(res.categories || []);
          this.links.set(res.links || []);
          this.apps.set(res.apps || []);
          if (res.categories && res.categories.length > 0) {
            if (!this.newLink.tabId) this.newLink.tabId = res.categories[0].id;
            if (!this.newApp.tabId) this.newApp.tabId = res.categories[0].id;
          }
        }
      }
    });
  }

  public closeConfig(): void {
    this.onDataChanged.emit();
    this.onClose.emit();
  }

  // --- CATÉGORIES ---
  public saveCategory(): void {
    if (!this.newCategory.id || !this.newCategory.name) return;

    const request = this.editingCategoryId
      ? this.http.put('/api/data/category', this.newCategory)
      : this.http.post('/api/data/category', this.newCategory);

    request.subscribe({
      next: () => {
        this.loadData();
        this.onDataChanged.emit();
        this.cancelCategoryEdit();
      }
    });
  }

  public editCategory(cat: Category): void {
    this.editingCategoryId = cat.id;
    this.newCategory = { ...cat };
  }

  public cancelCategoryEdit(): void {
    this.editingCategoryId = null;
    this.newCategory = { id: '', name: '', order: this.categories().length, icon: 'fa-star' };
  }

public deleteCategory(id: string): void {
    if (!confirm('Supprimer cette catégorie ?')) return;
    this.http.delete(`/api/data/category/${id}`).subscribe({
      next: () => {
        this.loadData();
        this.onDataChanged.emit();
      }
    });
  }

  // --- LIENS WEB ---
  public onLinkFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedLinkFile = file;
      const reader = new FileReader();
      reader.onload = () => this.linkPreviewUrl = reader.result as string;
      reader.readAsDataURL(file);
    }
  }

  public saveLink(): void {
    if (!this.newLink.name || !this.newLink.url || !this.newLink.tabId) return;

    const request = this.editingLinkId
      ? this.http.put('/api/data/link', this.newLink)
      : this.http.post<LinkItem>('/api/data/link', this.newLink);

request.subscribe({
      next: (res: any) => {
        const entityId = this.editingLinkId || (res && res.id);
        if (this.selectedLinkFile && entityId) {
          this.uploadIcon(entityId, false, this.selectedLinkFile, () => {
            this.resetLinkForm();
            this.onDataChanged.emit();
          });
        } else {
          this.resetLinkForm();
          this.onDataChanged.emit();
        }
      }
    });
  }

  public editLink(link: LinkItem): void {
    this.editingLinkId = link.id || null;
    this.newLink = { ...link };
    this.linkPreviewUrl = link.iconFileName ? `/api/public/images/${link.iconFileName}` : null;
  }

  public cancelLinkEdit(): void {
    this.resetLinkForm();
  }

  private resetLinkForm(): void {
    this.editingLinkId = null;
    this.newLink = { tabId: this.categories()[0]?.id || '', name: '', url: '' };
    this.selectedLinkFile = null;
    this.linkPreviewUrl = null;
    this.loadData();
  }

public deleteLink(id?: string): void {
    if (!id || !confirm('Supprimer ce lien web ?')) return;
    this.http.delete(`/api/data/link/${id}`).subscribe({
      next: () => {
        this.loadData();
        this.onDataChanged.emit();
      }
    });
  }

  // --- APPLICATIONS ---
  get formattedPath(): string {
    const path = this.newApp.path || 'C:\\Chemin\\Executable.exe';
    return path.replace(/\\/g, '\\\\');
  }

  copyConfigSnippet() {
    const appId = this.newApp.id || 'cle';
    const path = (this.newApp.path || 'C:\\Chemin\\Executable.exe').replace(/\\/g, '\\\\');
    const snippet = `,\n"${appId}": "${path}"`;

    navigator.clipboard.writeText(snippet).then(() => {
      console.log("Copié dans le presse-papiers !");
    });
  }

  public onAppFileSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedAppFile = file;
      const reader = new FileReader();
      reader.onload = () => this.appPreviewUrl = reader.result as string;
      reader.readAsDataURL(file);
    }
  }

  public saveApp(): void {
    if (!this.newApp.id || !this.newApp.name || !this.newApp.tabId) return;

    const request = this.editingAppId
      ? this.http.put('/api/data/app', this.newApp)
      : this.http.post('/api/data/app', this.newApp);

    request.subscribe({
      next: () => {
        if (this.selectedAppFile && this.newApp.id) {
          this.uploadIcon(this.newApp.id, true, this.selectedAppFile, () => {
            this.resetAppForm();
            this.onDataChanged.emit();
          });
        } else {
          this.resetAppForm();
          this.onDataChanged.emit();
        }
      }
    });
  }

  public editApp(app: AppItem): void {
    this.editingAppId = app.id;
    this.newApp = { ...app };
    this.appPreviewUrl = app.iconFileName ? `/api/public/images/${app.iconFileName}` : null;
  }

  public cancelAppEdit(): void {
    this.resetAppForm();
  }

  private resetAppForm(): void {
    this.editingAppId = null;
    this.newApp = { id: '', tabId: this.categories()[0]?.id || '', name: '', path: '' };
    this.selectedAppFile = null;
    this.appPreviewUrl = null;
    this.loadData();
  }

public deleteApp(id: string): void {
    if (!confirm('Supprimer cette application ?')) return;
    this.http.delete(`/api/data/app/${id}`).subscribe({
      next: () => {
        this.loadData();
        this.onDataChanged.emit();
      }
    });
  }

  private uploadIcon(entityId: string, isApp: boolean, file: File, callback: () => void): void {
    const formData = new FormData();
    formData.append('file', file);

    this.http.post(`/api/data/upload-icon/${entityId}?isApp=${isApp}`, formData).subscribe({
      next: () => callback(),
      error: () => callback()
    });
  }
}
