import React, { Component } from 'react';
import { connect } from 'react-redux';
import { Button } from 'react-bootstrap';

class ButtonConnection extends Component {
  constructor(props, context) {
    super(props, context);

    this.handleClick = this.handleClick.bind(this);

    this.state = {
      loading: false
    };
  }

  handleClick () {
    this.setState({ loading: true });

    // This probably where you would have an `ajax` call
    setTimeout(() => {
      // Completed of async action, set loading state back
      this.setState({ isLoading: false });
    }, 2000);
  }

  render() {

    let d = disabled(this.props);
    let l = label(this.props);
    let s = style(this.props);

    return <Button disabled={d} onClick={!d ? this.handleClick : null} bsStyle={s}>{l}</Button>;
  }
}

const disabled = (props) => {
  if (props.connected) return true;
  if (props.retry || props.connecting) return true;
  return false;
};

const style = (props) => {
  if (props.connected) return 'success';
  if (props.retry || props.connecting) return 'info';
  return 'warning';
};

const label = (props) => {
  return props.connected
    ? 'Connected'
    : props.retry
      ? `Disconnected. Retry in ${props.interval} seconds`
      : 'Click to connect';
};

const mapStateToProps = state => {
  return {
    connected: state.connection.connected,
    retry: state.connection.retry,
    connecting: state.connection.connecting,
    interval: 3
  };
};

const mapDispatchToProps = dispatch => {
  return {
    connect: () => dispatch({ type: 'connect' })
  };
};

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(ButtonConnection);