import { Component, Suspense, onMount } from "solid-js";
import { Router, Route } from "@solidjs/router";
import { lazy } from "solid-js";
import { useI18n, detectLocale } from "../lib/i18n";
import "./app.css";

const Home = lazy(() => import("./routes/home"));
const Auth = lazy(() => import("./routes/auth"));
const AuthCallback = lazy(() => import("./routes/auth-callback"));
const Dashboard = lazy(() => import("./routes/dashboard"));
const Social = lazy(() => import("./routes/social"));
const SocialProfile = lazy(() => import("./routes/social-profile"));
const IDE = lazy(() => import("./routes/ide"));
const Verification = lazy(() => import("./routes/verification"));

const App: Component = () => {
  const { changeLocale } = useI18n();

  onMount(() => {
    changeLocale(detectLocale());
  });

  return (
    <Router>
      <Suspense fallback={
        <div class="flex items-center justify-center min-h-screen bg-background text-foreground">
          <div class="animate-pulse text-secondary font-medium">Loading...</div>
        </div>
      }>
        <Route path="/" component={Home} />
        <Route path="/auth" component={Auth} />
        <Route path="/auth/callback" component={AuthCallback} />
        <Route path="/verification" component={Verification} />
        <Route path="/dashboard" component={Dashboard} />
        <Route path="/social" component={Social} />
        <Route path="/social/profile/:id" component={SocialProfile} />
        <Route path="/ide" component={IDE} />
      </Suspense>
    </Router>
  );
};

export default App;
