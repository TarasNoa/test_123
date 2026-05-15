import { onMount, type Component } from "solid-js";
import { useNavigate, useSearchParams } from "@solidjs/router";

const AuthCallback: Component = () => {
  const navigate = useNavigate();
  const [params] = useSearchParams();

  onMount(() => {
    const token = params.token || params.access_token;
    if (token) {
      localStorage.setItem("accessToken", token as string);
      navigate("/ide");
    } else {
      navigate("/auth");
    }
  });

  return (
    <div class="flex items-center justify-center min-h-screen bg-background text-foreground">
      <div class="animate-pulse text-primary font-medium">Authenticating...</div>
    </div>
  );
};

export default AuthCallback;
