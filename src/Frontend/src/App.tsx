import { Router, Route } from '@solidjs/router';
import Auth from './app/routes/auth';
import IDE from './app/routes/ide';
import Marketplace from './app/routes/marketplace';
import { createStore } from 'solid-js/store';

const [authState, setAuthState] = createStore({ isAuthenticated: false, user: null });

function ErrorBoundary(props) {
  const [error, setError] = createSignal(null);

  onError((err) => setError(err));

  return (
    <ErrorBoundary fallback={(err) => <div>Error: {err.message}</div>}>
      {props.children}
    </ErrorBoundary>
  );
}

function App() {
  return (
    <ErrorBoundary>
      <Router>
        <Route path="/" component={Auth} />
        <Route path="/ide" component={IDE} />
        <Route path="/marketplace" component={Marketplace} />
      </Router>
    </ErrorBoundary>
  );
}

export default App;