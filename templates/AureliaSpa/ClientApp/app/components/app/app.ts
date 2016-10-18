import {Aurelia} from 'aurelia-framework';
import {Router, RouterConfiguration} from 'aurelia-router';

export class App {
  router: Router;

  configureRouter(config: RouterConfiguration, router: Router) {
    config.title = 'Aurelia';
    config.map([
      { route: ['', 'home'], name: 'home', settings:{class:'home'},     moduleId: '../home/home',  nav: true, title: 'Home' },
      { route: 'counter',         name: 'counter', settings:{class:'education'},   moduleId: '../counter/counter',        nav: true, title: 'Counter' },
      { route: 'fetch-data',  name: 'fetchdata', settings:{class:'th-list'}, moduleId: '../fetchdata/fetchdata', nav: true, title: 'Fetch data' }
    ]);

    this.router = router;
  }
}

 