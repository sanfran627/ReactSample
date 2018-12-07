//var signalR = require('@aspnet/signalr/dist/browser/signalr.min.js');
//import signalR from '@aspnet/signalr';
var signalR = require('@aspnet/signalr');

var messageHandlers = {};
var messageHandlersE = {};
var timeoutRunning = false;
var retry = true;
var interval = 3;
var attempts = 0;
var token = '';
var connection = null;

const attempt = (reset) => {
  if ((reset || false)) {
    retry = true;
    attempts = 0;
    interval = 3;
  } else {
    switch (attempts) {
      case 0:
        retry = true;
        interval = 3;
        break;
      case 1:
        retry = true;
        interval = 10;
        break;
      case 2:
        retry = true;
        interval = 20;
        break;
      case 3:
        retry = true;
        interval = 30;
        break;
      default:
        retry = false;
        break;
    }
    attempts++;
  }
};

const initializeSignalR = (store, onConnected) => {
  // if (typeof connection !== 'undefined') return
  // console.log('initializing signalR')

  var path = process.env.NODE_ENV === 'development'
    ? 'http://localhost:5000/sitehub'
    : '/sitehub';

  if ((token || '') !== '') {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(path, { accessTokenFactory: () => { return token; } })
      .configureLogging(signalR.LogLevel.Information)
      .build();
  } else {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(path)
      .configureLogging(signalR.LogLevel.Information)
      .build();
  }

  registerEvents(store, connection, onConnected);
  startSignalR(store);
};

const startSignalR = (store, retry) => {
  const dispatch = store.dispatch.bind(store);
  // console.log('starting connection')
  dispatch({ type: 'connecting' });
  attempt(retry);
  connection.start()
    .catch(() => {
      dispatch({ type: 'reset' });
      // console.log('failed to start  ' + err.toString())
      if ((retry||false) && !timeoutRunning) {
        timeoutRunning = true;
        attempt();
        dispatch({ type: 'retry', interval: interval });
        setTimeout(() => {
          startSignalR(store,true);
          timeoutRunning = false;
        }, interval * 1000);
      }
    }).then(() => {
      // console.log('finished connecting')
    });
};

const registerEvents = (store, connection, onConnected) => {
  const dispatch = store.dispatch.bind(store);
  connection.on('connected', () => {
    // console.log('connected via signalR')
    dispatch({ type: 'connected' });
    if (typeof onConnected === 'function') onConnected();
    dispatch({ type: 'config' });
  });

  connection.on('config', (payload) => {
    let response = JSON.parse(payload);
    dispatch({ type: 'response', response: response.data });
  });

  connection.on('signup', (msgId, payload, t) => {
    let response = JSON.parse(payload);
    if (response.code === 0) {
      token = t;
      localStorage.setItem('token', token);
      if (typeof response.data !== 'undefined') {
        dispatch({ type: 'response', response: response.data });
      }
    } else {
      dispatch({ type: 'error', response: response });
    }

    handleCallback(msgId, response.code === 0, response);
  });

  connection.on('signin', (msgId, payload, t) => {
    // var size = typeof payload !== 'undefined' ? payload.length : 0
    // console.log(`receiving signalR response (login) => msgId: ${msgId}, payload size: ${size}, token: ${token}`)
    let response = JSON.parse(payload);
    if (response.code === 0) {
      token = t;
      localStorage.setItem('token', token);
      if (typeof response.data !== 'undefined') {
        dispatch({ type: 'response', response: response.data });
      }
    } else {
      dispatch({ type: 'error', response: response });
    }
    handleCallback(msgId, response.code === 0, response);
  });
  
  connection.on('check', (payload) => {
    // var size = typeof payload !== 'undefined' ? payload.length : 0
    // console.log(`receiving signalR response => message: check, payload size: ${size}`)
    let response = JSON.parse(payload);
    dispatch({ type: 'response', response: response.data });
  });

  connection.on('reset', (msgId, success) => {
    // console.log(`receiving signalR response => message: reset, msgId: ${msgId}, success: ${success}`)
    handleCallback(msgId, success);
  });

  connection.on('verify-email', (msgId, payload) => {
    let response = JSON.parse(payload);
    handleCallback(msgId, response.code === 0, response);
  });

  connection.on('response', (msgId, payload) => {
    // var size = typeof payload !== 'undefined' ? payload.length : 0
    // console.log(`receiving signalR response => msgId: ${msgId}, payload size: ${size}`)
    let response = JSON.parse(payload);
    if (response.code === 0) {
      if (typeof response.data !== 'undefined') {
        dispatch({ type: 'response', response: response.data });
      }
    } else {
      dispatch({ type: 'error', response: response });
    }
    handleCallback(msgId, response.code === 0, response);
  });

  connection.on('signout', () => {
    messageHandlers = {};
    messageHandlersE = {};
    dispatch({ type: 'signout' });
    initializeSignalR(store, () => {});
  });

  connection.onclose(async () => {
    // if there are pending handlers, process them all, then abort
    dispatch({ type: 'disconnected' });
    var rs = { code: -1, message: 'Network interruption during request. Please try again shortly' };
    for (var msgId in messageHandlers) {
      handleCallback(msgId, false, rs);
    }
    messageHandlers = {};
    messageHandlersE = {};
    await startSignalR(store);
  });
};

const config = (store) => {
  if (notConnected(store)) return;
  console.log(`sending signalR request => message: config`);
  connection.invoke('config').catch((err) => {
    console.log(err.toString());
  });
};

const signin = (store, credentials, callback) => {
  if (notConnected(store, callback)) return;
  var msgId = registerCallback(callback);
  // console.log(`sending signalR request => message: signin`)
  connection.invoke('signin', msgId, credentials).catch(() => {
    // console.log(err.toString())
  });

  return true;
};

const signup = (store, request, callback) => {
  if (notConnected(store, callback)) return;
  var msgId = registerCallback(callback);
  // console.log(`sending signalR request => message: reset`)
  connection.invoke('signup', msgId, request).catch(() => {
    // console.log(err.toString())
  });

  return true;
};

const check = (store) => {
  if (notConnected(store)) return;
  // console.log(`sending signalR request => message: check`)
  connection.invoke('check').catch(() => {
    // console.log(err.toString())
  });
};

const signout = (dispatch) => {
  dispatch('unsetError');
  dispatch('signout');
  initializeSignalR(dispatch);
};

const request = (store, request, callback) => {
  if (notConnected(store, callback)) return;

  var payload = JSON.stringify(request);
  // console.log(`sending signalR request => message: ${request.action}, payload size: ${payload.length}`)
  var msgId = registerCallback(callback);
  connection.invoke('request', msgId, payload).catch(err => {
    console.log(err.toString());
  });

  // purge any old messages
  var now = new Date();
  for (var prop in messageHandlersE) {
    if (messageHandlersE[prop] < now) {
      // console.log(`removing queued callback '${prop} from messageHandlers`)
      delete messageHandlers[prop];
      delete messageHandlersE[prop];
    }
  }
};

const guid = (a) => {
  var crypto = window.crypto || window.msCrypto;
  return a
    ? (a ^ crypto.getRandomValues(new Uint8Array(1))[0] % 16 >> a / 4).toString(16) // in hexadecimal
    : ([1e7] + 1e3 + 4e3 + 8e3 + 1e11).replace(/[018]/g, guid);
};

const registerCallback = (callback) => {
  var msgId = guid();
  if (typeof callback === 'function') {
    messageHandlers[msgId] = callback;
    var tPlus = new Date();
    tPlus.setMinutes(tPlus.getMinutes() + 1);
    messageHandlersE[msgId] = tPlus;
  }
  return msgId;
};

const notConnected = (store, callback) => {
  if (!store.getState().connection.connected) {
    if (typeof callback === 'function') {
      var rs = false;
      callback(rs);
      return true;
    } else {
      return true;
    }
  }
  return false;
};

const handleCallback = (msgId, success, response) => {
  if (typeof messageHandlers[msgId] !== 'undefined') {
    messageHandlers[msgId](success, response);
    delete messageHandlers[msgId];
    delete messageHandlersE[msgId];
  }
};

export const signalRMiddleware = store => next => action => {
  const dispatch = store.dispatch.bind(store);
  // var state = store.getState();

  try {
    switch (action.type) {

      case 'start':
        token = '';
        initializeSignalR(store, action.onConnected);
        return;
      case 'authenticated':
        token = action.token;
        initializeSignalR(store, action.onConnected);
        return;
      case 'stop':
        dispatch('reset');
        attempt(true);
        connection.stop();
        return;

      case 'check':
        check(store);
        return;

      case 'config':
        config(store);
        return;

      case 'request':
        request(store, action.callback);
        return;

      case 'signup':
        signup(store, action.request, action.callback);
        return;

      case 'signin':
        signin(store, action.credentials, action.callback);
        return;

      case 'signout':
        signout(store, action.callback);
        return;

      default:
        return next(action);
    }
  } catch (err) {
    console.log(err);
  }
};
