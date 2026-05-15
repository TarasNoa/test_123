import { type Component } from "solid-js";
import { useNavigate } from "@solidjs/router";

const Dashboard: Component = () => {
  const navigate = useNavigate();

  return (
    <div class="flex flex-col items-center justify-center min-h-screen bg-background text-foreground">
      <h1 class="text-2xl font-bold mb-4">Dashboard</h1>
      <p class="text-muted-foreground mb-6">Coming soon</p>
      <button
        onClick={() => navigate("/ide")}
        class="px-4 py-2 bg-primary text-primary-foreground rounded hover:bg-primary/90 transition-colors"
      >
        Open IDE
      </button>
    </div>
  );
};

export default Dashboard;
