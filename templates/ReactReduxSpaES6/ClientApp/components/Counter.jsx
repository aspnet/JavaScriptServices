import React, {Component, PropTypes} from 'react';
import { connect } from 'react-redux';
import { incrementCount } from '../reducers/counter';

class Counter extends Component {
  render() {
    return (
      <div>
        <h1>Counter</h1>
        <p>This is a simple example of a React component.</p>
        <p>Current count: <strong>{ this.props.count }</strong></p>
        <button onClick={ () => { this.props.increment() } }>Increment</button>
     </div>
    );
  }
}

Counter.propTypes = {
  count: PropTypes.number.isRequired,
  increment: PropTypes.func.isRequired,
};

const mapStateToProps = (state) => ({
  count: state.counter.count
});

const mapDispatchToProps = (dispatch) => ({
  increment: () => {
    dispatch(incrementCount());
  }
});

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(Counter);
