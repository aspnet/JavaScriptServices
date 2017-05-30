import { Component } from '@angular/core';

// Workaround for issue https://github.com/aspnet/JavaScriptServices/issues/960
import 'jquery';
import 'bootstrap';

@Component({
    selector: 'app',
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent {
}
