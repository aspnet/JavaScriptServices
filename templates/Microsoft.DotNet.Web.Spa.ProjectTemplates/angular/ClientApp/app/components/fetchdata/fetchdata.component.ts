import { Component, Inject } from '@angular/core';
import { Http } from '@angular/http';
import { trigger, style, transition, animate } from '@angular/animations';

@Component({
    selector: 'fetchdata',
    templateUrl: './fetchdata.component.html',
    animations: [
        trigger('itemAnim', [
            transition(':enter', [
                style({ transform: 'translateX(-100%)' }),
                animate(350)
            ])
        ])
    ]
})
export class FetchDataComponent {
    public forecasts: WeatherForecast[];

    constructor(http: Http, @Inject('BASE_URL') baseUrl: string) {
        http.get(baseUrl + 'api/SampleData/WeatherForecasts').subscribe(result => {
            this.forecasts = result.json() as WeatherForecast[];
        }, error => console.error(error));
    }
}

interface WeatherForecast {
    dateFormatted: string;
    temperatureC: number;
    temperatureF: number;
    summary: string;
}
