import { Component, OnInit, OnDestroy, Output, EventEmitter, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface SystemInfo {
  cpuUsage: number;
  memoryUsage: number;
  graphicUsage: number;
  uptimeSeconds: number;
}
interface BatteryInfo {
  level: number;
}

@Component({
  selector: 'app-header',
  standalone: false,
  templateUrl: './header.component.html',
  styleUrl: './header.component.css'
})
export class HeaderComponent implements OnInit, OnDestroy {
  @Output() public onOpenConfig = new EventEmitter<void>();

  public cpu = signal<number>(0);
  public ram = signal<number>(0);
  public gpu = signal<number>(0);
  public uptime = signal<string>('--');
  public currentDate = signal<string>('--');
  public currentTime = signal<string>('--:--');
  
  public kbBattery = signal<number>(100);
  public mouseBattery = signal<number>(100);
  public xboxBattery = signal<number>(100);

  private timer: any;
  private statsTimer: any;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.updateClock();
    this.timer = setInterval(() => this.updateClock(), 1000);
    this.loadStats();
    this.statsTimer = setInterval(() => this.loadStats(), 5000);
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
    clearInterval(this.statsTimer);
  }

  public triggerConfig(): void {
    console.log('Roue crantée cliquée !');
    this.onOpenConfig.emit();
  }

  private updateClock(): void {
    const now = new Date();
    this.currentTime.set(now.toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' }));
    this.currentDate.set(now.toLocaleDateString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric' }));
  }

  private loadStats(): void {
    this.http.get<SystemInfo>('/api/system/info').subscribe({
      next: (res) => {
        if (res) {
          this.cpu.set(res.cpuUsage);
          this.ram.set(res.memoryUsage);
          this.gpu.set(res.graphicUsage);
          this.uptime.set(this.formatUptime(res.uptimeSeconds));
        }
      },
      error: (err) => console.warn('Erreur stats system:', err)
    });

    this.http.get<BatteryInfo>('/api/device/keyboard-battery').subscribe({
      next: (res) => { if (res) this.kbBattery.set(res.level); },
      error: () => {}
    });

    this.http.get<BatteryInfo>('/api/device/mouse-battery').subscribe({
      next: (res) => { if (res) this.mouseBattery.set(res.level); },
      error: () => {}
    });

    this.http.get<BatteryInfo>('/api/device/xboxcontroller-battery').subscribe({
      next: (res) => { if (res) this.xboxBattery.set(res.level); },
      error: () => {}
    });
  }

  private formatUptime(seconds: number): string {
    const d = Math.floor(seconds / 86400);
    const h = Math.floor((seconds % 86400) / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const parts = [];
    if (d > 0) parts.push(`${d}j`);
    if (h > 0 || d > 0) parts.push(`${h}h`);
    parts.push(`${m}m`);
    return parts.join(' ');
  }

  public getUsageClass(val: number): string {
    if (val >= 90) return 'text-red-500';
    if (val >= 70) return 'text-orange-400';
    return 'text-teal-400';
  }

  public getBatteryClass(val: number): string {
    if (val < 0) return 'text-slate-500';
    if (val <= 20) return 'text-red-500';
    if (val <= 50) return 'text-orange-400';
    return 'text-teal-400';
  }
}
