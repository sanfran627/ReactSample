import React from 'react';
import { Button, FormGroup, ControlLabel, FormControl } from 'react-bootstrap';
import { connect } from 'react-redux';
import Cookies from 'universal-cookie';

const cookies = new Cookies();

class Signin extends React.Component {
  constructor(props, context) {
    super(props, context);

    this.handleChange = this.handleChange.bind(this);
    this.signinClick = this.signinClick.bind(this);

    var e = cookies.get('email');

    this.state = {
      email: e||'my@user.com',
      password: 'testing-1234!',
      language: 'en',
      staySignedIn: true,
      sending: false
    };
  }

  emailValidation = () => {
   // would normally do an email validation here.
   const length = this.state.email.length;
      if (length > 5) return 'success';
      else if (length < 5 && length > 0) return 'warning';
      else if (length === 0) return 'error';
    return null;
  }

  passwordValidation = () => {
   // just a basic check. nothing special. Would normally do regex validation, etc.
   const length = this.state.password.length;
      if (length > 8) return 'success';
      else if (length < 8 && length > 0) return 'warning';
      else if (length === 0) return 'error';
    return null;
  }

  handleChange = (e) => {
      this.setState({[e.target.name]: e.target.value});
  }

  disabled = () => {
    return this.state.sending;
  }

  resetClick = () => {
      this.props.history.push('/counter');
  }

  signinClick = () => {
    this.setState({ sending: true });

    var rq = {
      username: this.state.email,
      password: this.state.password,
      language: this.state.language
    };

    this.props.signin(rq, this.signinCallback);
  }

  signinCallback = () => {
      console.log(this.props.user);
    this.setState({ sending: false });
    if (this.props.user !== null) {
      // redirect to the main page
      console.log('redirecting!');
      if (this.state.staySignedIn) {
        cookies.set('email', this.state.email);
      } else {
        cookies.remove('email');
      }
      this.props.history.push('/dashboard');
    } else {
      //show error
    }
  }

  getText = (field, defaultValue) => {
    // if multilingual is required, have the language be set based on the user object or a cookie, 
    // use a .json file to do strings based on language (or just to extrapolate out strings entirely,
    // so they're not in the site.
    // poor version here:

    switch (field) {
      case 'form.signin.label.email': return 'Email Address';
      case 'form.signin.label.password': return 'Password';
      case 'form.signin.place.email': return '';
      case 'form.signin.place.password': return '';
      default: return defaultValue;
    }
  }

  render = () => {

    const wellStyles = { maxWidth: 400, margin: '0 auto 10px' };

    const buttons = (
      <div style={wellStyles}>
        <Button bsStyle="primary" block onClick={this.signinClick} disabled={this.disabled()}>
          Sign In
        </Button>
        <Button block onClick={this.resetClick}>
          Reset Password
        </Button>
      </div>
    );

    return (
      <form>
        <FormGroup controlId="email" validationState={this.emailValidation()}>
          <ControlLabel>{this.getText('form.signin.label.email', 'Email Address')}</ControlLabel>
          <FormControl
              type="email"
              name="email"
              value={this.state.email}
              placeholder={this.getText('form.signin.place.email', '')}
              onChange={this.handleChange}
          />
          <FormControl.Feedback />
        </FormGroup>

        <FormGroup controlId="password" validationState={this.passwordValidation()}>
          <ControlLabel>{this.getText('form.signin.label.password', 'Password')}</ControlLabel>
          <FormControl
              type="password"
              name="password"
              value={this.state.password}
              placeholder={this.getText('form.signin.place.email', '')}
              onChange={this.handleChange}
          />
          <FormControl.Feedback />
        </FormGroup>
        {buttons}
      </form>
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
    signin: (credentials, callback) => dispatch({ type: 'signin', credentials, callback })
  };
};

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(Signin);
