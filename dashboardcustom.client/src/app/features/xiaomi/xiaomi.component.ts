import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface LightStatus {
  poweredOn: boolean;
  brightness: number;
  r: number;
  g: number;
  b: number;
  nameSelected: string;
}

@Component({
  selector: 'app-xiaomi',
  standalone: false,
  templateUrl: './xiaomi.component.html',
  styleUrl: './xiaomi.component.css'
})
export class XiaomiComponent implements OnInit, OnDestroy {
  public poweredOn = signal<boolean>(false);
  public brightness = signal<number>(0);
  private brightnessDebounce: any;
  private refreshTimer: any;

  public colors = [
    { r: 255, g: 255, b: 255, hex: '#ffffff' },
    { r: 234, g: 88, b: 12, hex: '#ea580c' },
    { r: 56, g: 189, b: 248, hex: '#38bdf8' },
    { r: 225, g: 29, b: 72, hex: '#e11d48' },
    { r: 74, g: 222, b: 128, hex: '#4ade80' },
    { r: 96, g: 165, b: 250, hex: '#60a5fa' },
    { r: 168, g: 85, b: 247, hex: '#a855f7' },
    { r: 236, g: 72, b: 153, hex: '#ec4899' },
    { r: 234, g: 179, b: 8, hex: '#eab308' },
    { r: 45, g: 212, b: 191, hex: '#2dd4bf' },
    { r: 241, g: 245, b: 249, hex: '#f1f5f9' },
    { r: 79, g: 70, b: 229, hex: '#4f46e5' }
  ];

  public currentColorHex = signal<string>('#ffffff');

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadStatus();
    this.refreshTimer = setInterval(() => this.loadStatus(), 5000);
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
    }
  }

  public loadStatus(): void {
    this.http.get<LightStatus>('/api/light/current').subscribe({
      next: (res) => {
        if (res) {
          this.poweredOn.set(res.poweredOn);
          this.brightness.set(res.brightness);
          this.currentColorHex.set(this.rgbToHex(res.r, res.g, res.b));
        }
      },
      error: (err) => console.warn(err)
    });
  }

  public togglePower(): void {
    this.http.post('/api/light/toggle', {}).subscribe({
      next: () => this.poweredOn.update(v => !v),
      error: (err) => console.warn(err)
    });
  }

  public onBrightnessChange(event: any): void {
    const val = parseInt(event.target.value, 10);
    this.brightness.set(val);
    clearTimeout(this.brightnessDebounce);
    this.brightnessDebounce = setTimeout(() => {
      this.http.post(`/api/light/brightness?value=${val}`, {}).subscribe({
        error: (err) => console.warn(err)
      });
    }, 150);
  }

  public setColor(col: { r: number, g: number, b: number, hex: string }): void {
    this.currentColorHex.set(col.hex);
    this.http.post(`/api/light/color?r=${col.r}&g=${col.g}&b=${col.b}`, {}).subscribe({
      error: (err) => console.warn(err)
    });
  }

  private rgbToHex(r: number, g: number, b: number): string {
    return "#" + [r, g, b].map(x => x.toString(16).padStart(2, '0')).join('');
  }
}
