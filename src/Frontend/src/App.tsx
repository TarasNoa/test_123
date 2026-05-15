import { Component, Suspense, onMount } from "solid-js";
import { Router, Route } from "@solidjs/router";
import { lazy } from "solid-js";
import { useI18n, detectLocale } from "./lib/i18n";
import "./app/app.css";

const Home = lazy(() => import("./app/routes/home"));
const Auth = lazy(() => import("./app/routes/auth"));
const AuthCallback = lazy(() => import("./app/routes/auth-callback"));
const Dashboard = lazy(() => import("./app/routes/dashboard"));
const IDE = lazy(() => import("./app/routes/ide"));

const App: Component = () => {
  const { changeLocale } = useI18n();

  onMount(() => {
    changeLocale(detectLocale());
  });

  return (
    <Router>
      <Suspense fallback={
        <div class="flex items-center justify-center min-h-screen bg-background text-foreground">
          <div class="animate-pulse text-primary font-medium">Loading...</div>
        </div>
      }>
        <Route path="/" component={Home} />
        <Route path="/auth" component={Auth} />
        <Route path="/auth/callback" component={AuthCallback} />
        <Route path="/dashboard" component={Dashboard} />
        <Route path="/ide" component={IDE} />
      </Suspense>
    </Router>
  );
};

export default App;