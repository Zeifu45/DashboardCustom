import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { NgModule, provideBrowserGlobalErrorListeners } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { FormsModule } from '@angular/forms';
import { AppRoutingModule } from './app-routing-module';
import { App } from './app';
import { apiKeyInterceptor } from './core/interceptors/api-key.interceptor';
import { SidebarComponent } from './features/sidebar/sidebar.component';
import { HeaderComponent } from './features/header/header.component';
import { AudioComponent } from './features/audio/audio.component';
import { XiaomiComponent } from './features/xiaomi/xiaomi.component';
import { DataViewComponent } from './features/data-view/data-view.component';
import { ConfigComponent } from './features/config/config.component';

@NgModule({
  declarations: [
    App,
    SidebarComponent,
    HeaderComponent,
    AudioComponent,
    XiaomiComponent,
    DataViewComponent,
    ConfigComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    FormsModule
  ],
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideHttpClient(withInterceptors([apiKeyInterceptor]))
  ],
  bootstrap: [App]
})
export class AppModule { }
