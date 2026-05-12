import { Router, Route } from '@solidjs/router';
import { ErrorBoundary } from 'solid-js';
import Auth from './app/routes/auth';
import IDE from './app/routes/ide';
import Marketplace from './app/routes/marketplace';

function App() {
  return (
    <ErrorBoundary fallback={(err) => <div>Error: {err.message}</div>}>
      <Router>
        <Route path="/" component={Auth} />
        <Route path="/dashboard" component={IDE} />
        <Route path="/ide" component={IDE} />
        <Route path="/marketplace" component={Marketplace} />
      </Router>
    </ErrorBoundary>
  );
}

export default App;