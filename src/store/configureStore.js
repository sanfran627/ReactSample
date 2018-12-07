import { applyMiddleware, combineReducers, compose, createStore } from 'redux';
import thunk from 'redux-thunk';
import { routerReducer, routerMiddleware } from 'react-router-redux';

import * as Connection from './Connection';
import * as Counter from './Counter';
import * as WeatherForecasts from './WeatherForecasts';

import { signalRMiddleware } from '../middleware/signalR';

export default function configureStore(history, initialState) {
  const reducers = {
    connection: Connection.reducer,
    counter: Counter.reducer,
    weatherForecasts: WeatherForecasts.reducer
  };
  
  const middleware = [
    thunk,
    signalRMiddleware,
    routerMiddleware(history)
  ];

  // In development, use the browser's Redux dev tools extension if installed
  const enhancers = [];
  const isDevelopment = process.env.NODE_ENV === 'development';
  if (isDevelopment && typeof window !== 'undefined' && window.devToolsExtension) {
    enhancers.push(window.devToolsExtension());
  }

  const rootReducer = combineReducers({
    ...reducers,
    routing: routerReducer
  });

  return createStore(
    rootReducer,
    initialState,
    compose(applyMiddleware(...middleware), ...enhancers)
  );
}
