import React from 'react';
import { connect } from 'react-redux';
import { Glyphicon, Nav, Navbar, NavItem } from 'react-bootstrap';
import { LinkContainer } from 'react-router-bootstrap';
import ButtonConnection from './ButtonConnection';

class Navigator extends React.Component {

  render = () => {

    var u = this.props.user || null;
    var h = u !== null;
    var d = h ? u.displayName : '';

    return (
      <Navbar inverse collapseOnSelect>
        <Navbar.Header>
          <Navbar.Brand>
            <a href="#brand">{h ? `Hi ${d}` : 'React-Bootstrap'}</a>
          </Navbar.Brand>
          <Navbar.Toggle />
        </Navbar.Header>
        <Navbar.Collapse>
          <Nav>
            <LinkContainer to={h ? '/dashboard' : '/'} exact>
              <NavItem>
                <Glyphicon glyph='home' /> { h ? 'Dashboard' : 'Signin'}
        </NavItem>
            </LinkContainer>
            <LinkContainer to={'/counter'}>
              <NavItem>
                <Glyphicon glyph='education' /> Counter
        </NavItem>
            </LinkContainer>
            <LinkContainer to={'/fetchdata'}>
              <NavItem>
                <Glyphicon glyph='th-list' /> Fetch data
        </NavItem>
            </LinkContainer>
          </Nav>
          <Nav pullRight>
            <NavItem href="#">
              <ButtonConnection />
            </NavItem>
          </Nav>
        </Navbar.Collapse>
      </Navbar>
    );
  }
}
const mapStateToProps = state => {
  return {
    user: state.connection.user
  };
};

const mapDispatchToProps = dispatch => {
  return {
    signout: () => dispatch({ type: 'signout' })
  };
};

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(Navigator);
