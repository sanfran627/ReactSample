import React, { Component } from 'react';
import { connect } from 'react-redux';
import { Alert } from 'react-bootstrap';

class ErrorAlert extends Component {
  constructor(props, context) {
    super(props, context);
    this.onDismiss = this.onDismiss.bind(this);
  }

  onDismiss = () => {
    this.props.dismiss();
  }

  render = () => {
    return this.props.error !== null
      ? <Alert bsStyle="danger" onDismiss={this.onDismiss}>{this.props.error.message}</Alert>
      : null;
  }
}

const mapStateToProps = state => {
  return {
    error: state.connection.error
  };
};

const mapDispatchToProps = dispatch => {
  return {
    dismiss: () => dispatch({ type: 'dismiss' })
  };
};

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(ErrorAlert);