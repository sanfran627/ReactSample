import React from 'react';
import { connect } from 'react-redux';

class Dashboard extends React.Component {
  constructor(props, context) {
    super(props, context);
  }

  componentDidMount () {
    if (this.props.user === null) {
      this.props.history.replace('/');
    }
  }

  render = () => {

    var n = (this.props.user || null) !== null ? this.props.user.displayName : ''

    return (
      <div>
        <h1>Hello {n}!</h1>

        <p>This is a simple example of a React component.</p>

      </div>
    );
  }
}

const mapStateToProps = state => {
  return {
    user: state.connection.user
  };
};

export default connect(
  mapStateToProps
)(Dashboard);
