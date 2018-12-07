import React from 'react';
import { Route } from 'react-router';
import Layout from './components/Layout';
import Signin from './components/Signin';
import Counter from './components/Counter';
import Dashboard from './components/Dashboard';
import FetchData from './components/FetchData';

const app = () => (
  <Layout>
    <Route exact path='/' component={Signin} />
    <Route path='/counter' component={Counter} />
    <Route path='/dashboard' component={Dashboard} />
    <Route path='/fetchdata/:startDateIndex?' component={FetchData} />
  </Layout>
);

export default app;