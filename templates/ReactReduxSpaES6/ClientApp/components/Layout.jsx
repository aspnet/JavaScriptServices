import React, {Component, PropTypes} from 'react';
import NavMenu from './NavMenu';

class Layout extends Component {
  render() {
    return (
      <div className='container-fluid'>
        <div className='row'>
          <div className='col-sm-3'>
            <NavMenu />
          </div>
          <div className='col-sm-9'>
            {this.props.body}
          </div>
        </div>
      </div>
    );
  }
}

Layout.propTypes = {
  body: PropTypes.element,
};

export default Layout;
