import { Component } from "solid-js";
import { Routes, Route } from "@solidjs/router";
import { lazy } from "solid-js";
import "./app.css";

// Lazy load routes for code splitting
const Home = lazy(() => import("./routes/home"));
const IDE = lazy(() => import("./routes/ide"));
const Settings = lazy(() => import("./routes/settings"));

const App: Component = () => {
  return (
    <div class="min-h-screen bg-background text-foreground">
      <Routes>
        <Route path="/" component={Home} />
        <Route path="/ide" component={IDE} />
        <Route path="/settings" component={Settings} />
      </Routes>
    </div>
  );
};

export default App;
