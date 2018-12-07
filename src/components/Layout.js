import React, { Component } from 'react';
import { Col, Grid, Row } from 'react-bootstrap';
import { bindActionCreators } from 'redux';
import { connect } from 'react-redux';
import { actionCreators } from '../store/Connection';
import NavMenu from './NavMenu';
import ErrorAlert from './ErrorAlert';

class Layout extends Component {
  componentWillMount() {
    this.props.connect();
  }

  render() {
    return (
      <div>
        <NavMenu />
        <ErrorAlert />
        <Grid>
          <Row>
            <Col md={4} sm={2} xs={0} />
            <Col md={4} sm={8} xs={12} >
              {this.props.children}
            </Col>
            <Col md={4} sm={2} xs={0} />
          </Row>
        </Grid>
      </div>
    );
  }
}

export default connect(
  state => state.connection,
  dispatch => bindActionCreators(actionCreators, dispatch)
)(Layout);
