import { combineReducers } from 'redux';
import { routerReducer } from 'react-router-redux'

import counter from './counter';
import weatherForecasts from './weatherForecasts';

export default combineReducers({
  routing: routerReducer,
  counter,
  weatherForecasts
});
