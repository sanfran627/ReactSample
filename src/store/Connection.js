const setup = 'setup';
const reset = 'reset';
const retry = 'retry';
const start = 'start';
const disconnected = 'disconnected';
const connecting = 'connecting';
const connected = 'connected';
const config = 'config';
const signup = 'signup';
const signin = 'signin';
const signout = 'signout';
const check = 'check';
const response = 'response';
const error = 'error';
const dismiss = 'dismiss';

const initialState = (connecting, token) => {
  return {
    retry: false,
    interval: 3,
    connecting: connecting || false,
    connected: false,
    token: token || '',
    user: null,
    error: null
  };
};

export const actionCreators = {
  setup: () => ({ type: setup }),
  reset: () => ({ type: reset }),
  retry: (interval) => ({ type: retry, interval }),
  connect: (token) => ({ type: start, token }),
  connecting: () => ({ type: connecting }),
  connected: () => ({ type: connected }),
  disconnected: () => ({ type: disconnected }),
  config: () => ( { type: config }),
  signup: () => ( { type: signup }),
  signin: () => ( { type: signin }),
  signout: () => ( { type: signout }),
  check: () => ({ type: check }),
  response: (response) => ({ type: response, response }),
  error: (error) => ({ type: error, error }),
  dismiss: () => ({ type: dismiss })
};

export const reducer = (state, action) => {
  state = state || initialState();

  switch (action.type) {

    case start:
      return { ...state, connecting: false, token: action.token, error: null };

    case reset:
      return { ...state, connected: false, connecting: false, retry: true, error: null };

    case retry:
      return { ...state, retry: true, interval: action.interval };

    case connecting:
      return { ...state, connecting: true, connected: false};

    case connected:
      return { ...state, connecting: false, connected: true};

    case disconnected:
      return { ...state, connecting: false, connected: false};

    case signout:
      return initialState();

    case dismiss:
      return { ...state, error: null };

    case error:
      return { ...state, error: action.response };

    case response:
      var s = Object.assign({}, state);
      for (var prop in action.response) {
        s[prop] = action.response[prop];
      }
      return s;

    default:
      return state;
  }
};
