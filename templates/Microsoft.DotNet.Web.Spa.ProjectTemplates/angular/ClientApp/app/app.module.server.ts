import { NgModule } from '@angular/core';
import { ServerModule } from '@angular/platform-server';
import { AppModuleShared } from './app.module.shared';
import { AppComponent } from './components/app/app.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

@NgModule({
    bootstrap: [ AppComponent ],
    imports: [
        ServerModule,
        AppModuleShared,
        NoopAnimationsModule
    ]
})
export class AppModule {
}
