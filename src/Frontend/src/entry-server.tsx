import { createHandler, StartServer } from "@solidjs/start/server";

export default createHandler(() => (
  <StartServer
    document={({ assets, children, scripts }) => (
      <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <meta http-equiv="content-security-policy" content="default-src 'self'; script-src 'self' 'unsafe-eval' 'unsafe-inline' http: https:; connect-src 'self' ws: wss: http: https: localhost:5000 localhost:5001 localhost:5002 localhost:5004 localhost:5007 localhost:3000; img-src 'self' data: blob: http: https:; style-src 'self' 'unsafe-inline'; font-src 'self' data:;" />
          {assets}
        </head>
        <body>
          <div id="app">{children}</div>
          {scripts}
        </body>
      </html>
    )}
  />
));
