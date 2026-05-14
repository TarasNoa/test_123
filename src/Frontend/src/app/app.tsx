import { Component, Suspense } from "solid-js";
import { Router, Route } from "@solidjs/router";
import { lazy } from "solid-js";
import "./app.css";

const Home = lazy(() => import("./routes/home"));
const Auth = lazy(() => import("./routes/auth"));
const IDE = lazy(() => import("./routes/ide"));
const Marketplace = lazy(() => import("./routes/marketplace"));
const Analytics = lazy(() => import("./routes/analytics"));
const Chat = lazy(() => import("./routes/chat"));
const Platform = lazy(() => import("./routes/platform"));
const Social = lazy(() => import("./routes/social"));
const Collaboration = lazy(() => import("./routes/collaboration"));
const Settings = lazy(() => import("./routes/settings"));

const App: Component = () => {
  return (
    <Router>
      <Suspense fallback={
        <div class="flex items-center justify-center min-h-screen bg-background text-foreground">
          <div class="animate-pulse text-primary font-medium">Loading...</div>
        </div>
      }>
        <Route path="/" component={Home} />
        <Route path="/auth" component={Auth} />
        <Route path="/ide" component={IDE} />
        <Route path="/marketplace" component={Marketplace} />
        <Route path="/analytics" component={Analytics} />
        <Route path="/chat" component={Chat} />
        <Route path="/platform" component={Platform} />
        <Route path="/social" component={Social} />
        <Route path="/collaboration" component={Collaboration} />
        <Route path="/settings" component={Settings} />
      </Suspense>
    </Router>
  );
};

export default App;
