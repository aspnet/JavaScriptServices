import { createStore, compose, applyMiddleware } from 'redux';
import thunkMiddleware from 'redux-thunk';
import rootReducer from './reducers'

export default (initialState) => {
  const windowIfDefined = typeof window === 'undefined' ? null : window;
  const devToolsExtension = windowIfDefined && windowIfDefined.devToolsExtension; // If devTools is installed, connect to it
  const createStoreWithMiddleware = compose(
    applyMiddleware(thunkMiddleware),
    devToolsExtension ? devToolsExtension() : f => f
  )(createStore);

  const store = createStoreWithMiddleware(rootReducer, initialState);
  // Enable Webpack hot module replacement for reducers
  if (module.hot) {
    module.hot.accept('./reducers', () => {
      store.replaceReducer(require('./reducers'));
    });
  }

  return store;
};
