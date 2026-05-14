import { mount, StartClient } from "@solidjs/start/client";
import App from "./app/app";

mount(() => <StartClient><App /></StartClient>, document.getElementById("app"));
