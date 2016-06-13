import React from 'React';
import {Router, Route, HistoryBase} from 'react-router';
import Layout from './components/layout';
import Home from './components/home';
import FetchData from './components/fetchData';
import Counter from './components/counter';

export default
  <Route component={Layout}>
    <Route path='/' components={{body: Home}} />
    <Route path='/counter' components={{ body: Counter }} />
    <Route path='/fetchdata' components={{ body: FetchData }}>
      <Route path=':startDateIndex' /> { /* Optional route segment that does not affect NavMenu highlighting */ }
    </Route>
  </Route>;
