import { fetch } from 'domain-task/fetch';

// Constant defining the action type
const REQUEST_WEATHER_FORECASTS = 'REQUEST_WEATHER_FORECASTS';
const RECEIVE_WEATHER_FORECASTS = 'RECEIVE_WEATHER_FORECASTS';

// Action creators
export const requestWeatherForecasts = (startDateIndex) => {
  return (dispatch, getState) => {
    if (startDateIndex !== getState().weatherForecasts.startDateIndex) {
      dispatch({type: REQUEST_WEATHER_FORECASTS, payload: startDateIndex});
      return fetch(`/api/SampleData/WeatherForecasts?startDateIndex=${ startDateIndex }`)
        .then(response => response.json())
        .then((data) => {
          dispatch(receiveWeatherForecasts(startDateIndex, data));
        });
    }
  };
};

export const receiveWeatherForecasts = (startDateIndex, forecasts) => ({
  type: RECEIVE_WEATHER_FORECASTS,
  payload: {startDateIndex, forecasts},
});

const initialState = {
  startDateIndex: null,
  forecasts: [],
  isLoading: false
};

// The reducer that changes the state based on the action type
export default (state = initialState, action) => {

  if (action.type === REQUEST_WEATHER_FORECASTS) {
    return { startDateIndex: action.payload, isLoading: true, forecasts: state.forecasts };
  } else if (action.type === RECEIVE_WEATHER_FORECASTS) {
    // Only accept the incoming data if it matches the most recent request. This ensures we correctly
    // handle out-of-order responses.
    if (action.payload.startDateIndex === state.startDateIndex) {
      return { startDateIndex: action.payload.startDateIndex, forecasts: action.payload.forecasts, isLoading: false };
    }
  }

  return state;
}
