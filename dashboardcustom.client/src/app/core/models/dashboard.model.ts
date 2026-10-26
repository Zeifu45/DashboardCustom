export interface Category {
  id: string;
  name: string;
  order: number;
  icon?: string;
}

export interface LinkItem {
  id: string;
  tabId: string;
  url: string;
  name: string;
  iconFileName: string;
}

export interface AppItem {
  id: string;
  tabId: string;
  appKey: string;
  name: string;
  iconFileName: string;
}

export interface DashboardData {
  categories: Category[];
  links: LinkItem[];
  apps: AppItem[];
}
