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
    this.signinCallback = this.signinCallback.bind(this);

    var e = cookies.get('email');

    this.state = {
      email: e||'my@user.com',
      password: 'testing-1234!',
      language: 'en',
      staySignedIn: true,
      sending: false,
      success: false
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
    var self = this;
    self.setState({ sending: true });

    var rq = {
      username: self.state.email,
      password: self.state.password,
      language: self.state.language
    };

    // send the signinup to the websocket

    // (to test this, simply bypass the signin callback)
    self.props.signin(rq, self.signinCallback);
    
    // allow for a maximum of 5 seconds to signin before aborting
    setTimeout(() => {
      // verify if still sending
      if (self.state.sending) {
        // reset state
        self.setState({ sending: false });
        // if we're here, show an error
        self.props.error(1,`We didn't receive a response in a timely manner. Please try again. If the problem persists, there is likely a system error and we'll address is immediately.`);
      }

    }, 5 * 1000);
  }

  // eslint-disable-next-line
  signinCallback = () => { 
    // process the callback ONLY if we're still sending
    if (!this.state.sending) {
      // do nothing - we passed the timeout. Let the user try again if they so desire
      return;
    }

    // update sending status
    this.setState({ sending: false });

    // if we received a user object in the response, it'll be in state. 
    if (this.props.user !== null) {
      // save the username/email via a cookie
      if (this.state.staySignedIn) {
        cookies.set('email', this.state.email);
      } else {
        cookies.remove('email');
      }
      // redirect to the main page
      this.props.history.push('/dashboard');
    }
    // if there is an error, it will display automatically
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
    signin: (credentials, callback) => dispatch({ type: 'signin', credentials, callback }),
    error: (code, message) => dispatch({ type: 'error', response: { code, message } })
  };
};

export default connect(
  mapStateToProps,
  mapDispatchToProps
)(Signin);
