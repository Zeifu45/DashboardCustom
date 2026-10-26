import { Component, OnInit, OnDestroy, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';

interface AudioInfo {
  nameSelected: string;
  volume: number;
}

@Component({
  selector: 'app-audio',
  standalone: false,
  templateUrl: './audio.component.html',
  styleUrl: './audio.component.css'
})
export class AudioComponent implements OnInit, OnDestroy {
  public volume = signal<number>(0);
  public nameSelected = signal<string>('');
  private volumeDebounce: any;
  private refreshTimer: any;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadAudioStatus();
    this.refreshTimer = setInterval(() => this.loadAudioStatus(), 5000);
  }

  ngOnDestroy(): void {
    if (this.refreshTimer) {
      clearInterval(this.refreshTimer);
    }
  }

  public loadAudioStatus(): void {
    this.http.get<AudioInfo>('/api/audio/current').subscribe({
      next: (res) => {
        if (res) {
          this.volume.set(res.volume);
          this.nameSelected.set(res.nameSelected || '');
        }
      },
      error: (err) => console.warn('Erreur récupération audio:', err)
    });
  }

  public setDevice(deviceName: string): void {
    this.http.post(`/api/audio/set?name=${deviceName}`, {}).subscribe({
      next: () => this.loadAudioStatus(),
      error: (err) => console.warn('Erreur changement périphérique audio:', err)
    });
  }

  public onVolumeChange(event: any): void {
    const val = parseInt(event.target.value, 10);
    this.volume.set(val);
    clearTimeout(this.volumeDebounce);
    this.volumeDebounce = setTimeout(() => {
      this.http.post(`/api/audio/volume?volume=${val}`, {}).subscribe({
        error: (err) => console.warn('Erreur modification volume:', err)
      });
    }, 150);
  }

  public isG560(): boolean {
    return this.nameSelected().toLowerCase().includes('g560');
  }

  public isRealtek(): boolean {
    return this.nameSelected().toLowerCase().includes('realtek');
  }
}
