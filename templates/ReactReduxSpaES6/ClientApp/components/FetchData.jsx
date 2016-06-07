import React, {Component, PropTypes} from 'react';
import { connect } from 'react-redux';
import { Link } from 'react-router';
import { requestWeatherForecasts } from '../reducers/weatherForecasts'

class FetchData extends Component {
  componentWillMount() {
    // This method runs when the component is first added to the page
    let startDateIndex = parseInt(this.props.params.startDateIndex) || 0;
    this.props.requestWeatherForecasts(startDateIndex);
  }

  componentWillReceiveProps(nextProps) {
    // This method runs when incoming props (e.g., route params) change
    let startDateIndex = parseInt(nextProps.params.startDateIndex) || 0;
    this.props.requestWeatherForecasts(startDateIndex);
  }

  render() {
    const table = (
      <table className='table'>
        <thead>
          <tr>
            <th>Date</th>
            <th>Temp. (C)</th>
            <th>Temp. (F)</th>
            <th>Summary</th>
          </tr>
        </thead>
        <tbody>
          {this.props.forecasts.map(forecast =>
            <tr key={ forecast.dateFormatted }>
              <td>{ forecast.dateFormatted }</td>
              <td>{ forecast.temperatureC }</td>
              <td>{ forecast.temperatureF }</td>
              <td>{ forecast.summary }</td>
            </tr>
          )}
        </tbody>
      </table>
    );

    let prevStartDateIndex = this.props.startDateIndex - 5;
    let nextStartDateIndex = this.props.startDateIndex + 5;
    const pagination = (
      <p className='clearfix text-center'>
        <Link className='btn btn-default pull-left' to={ `/fetchdata/${ prevStartDateIndex }` }>Previous</Link>
        <Link className='btn btn-default pull-right' to={ `/fetchdata/${ nextStartDateIndex }` }>Next</Link>
        { this.props.isLoading ? <span>Loading...</span> : [] }
      </p>
    );

    return (
      <div>
        <h1>Weather forecast</h1>
        <p>This component demonstrates fetching data from the server and working with URL parameters.</p>
        { table }
        { pagination }
      </div>
    );
  }
}

FetchData.propTypes = {
  startDateIndex: PropTypes.number,
  isLoading: PropTypes.bool,
  requestWeatherForecasts: PropTypes.func.isRequired
};

const mapStateToProps = (state) => ({
  startDateIndex: state.weatherForecasts.startDateIndex,
  forecasts: state.weatherForecasts.forecasts,
  isLoading: state.weatherForecasts.isLoading
});

const mapDispatchToProps = (dispatch) => ({
  requestWeatherForecasts: (startDateIndex) => {
    dispatch(requestWeatherForecasts(startDateIndex));
  }
});

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(FetchData);
