import * as React from 'react';
import { renderToString } from 'react-dom/server';
import { Provider } from 'react-redux';
import { match, RouterContext } from 'react-router';

import routes from './routes';
import configureStore from './configureStore';

export default (params) => {
  return new Promise((resolve, reject) => {
    match({routes, location: params.location}, (error, redirectLocation, renderProps) => {
      if (error) {
        throw error;
      }
      // At this point if we want to initialize the store we need to pass an object with shape
      // {counter: {count: 10}}
      const store = configureStore();
        const app = (
          <Provider store={ store }>
            <RouterContext {...renderProps} />
          </Provider>
      );

      // Perform an initial render that will cause any async tasks (e.g., data access) to begin
      renderToString(app);

      // Once the tasks are done, we can perform the final render
      // We also send the redux store state, so the client can continue execution where the server left off
      params.domainTasks.then(() => {
        resolve({
            html: renderToString(app),
            globals: { initialReduxState: store.getState() }
        });
      }, reject); // Also propagate any errors back into the host application
    });
  });
};
