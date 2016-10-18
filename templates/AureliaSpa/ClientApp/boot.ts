import {Aurelia} from 'aurelia-framework';
import 'bootstrap/dist/css/bootstrap.css';
import 'bootstrap';
 
// comment out if you don't want a Promise polyfill (remove also from webpack.config.js)
import * as Bluebird from  'bluebird';
Bluebird.config({ warnings: false });

export function configure(aurelia: Aurelia) {
  aurelia.use
    .standardConfiguration()
    .developmentLogging();

  aurelia.start().then(() => aurelia.setRoot('app/components/app/app'));
}