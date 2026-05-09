import { Component, Suspense } from "solid-js";
import { Router, Route } from "@solidjs/router";
import { lazy } from "solid-js";
import "./app.css";

const Home = lazy(() => import("./routes/home"));
const IDE = lazy(() => import("./routes/ide"));
const Settings = lazy(() => import("./routes/settings"));

const App: Component = () => {
  return (
    <Router>
      <Suspense fallback={<div class="p-8">Loading...</div>}>
        <Route path="/" component={Home} />
        <Route path="/ide" component={IDE} />
        <Route path="/settings" component={Settings} />
      </Suspense>
    </Router>
  );
};

export default App;
