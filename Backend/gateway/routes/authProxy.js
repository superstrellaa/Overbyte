const { createProxyMiddleware } = require("http-proxy-middleware");
const logger = require("@overbyte-backend/shared-logger");

module.exports = (app) => {
  const authTarget =
    process.env.NODE_ENV === "production"
      ? `http://${process.env.AUTH_SERVICE_HOST}:${process.env.AUTH_SERVICE_PORT}`
      : `http://localhost:3002`;

  app.use(
    "/auth/user",
    createProxyMiddleware({
      target: `${authTarget}/auth/user`,
      changeOrigin: true,
      pathRewrite: { "^/auth/user": "" },
      on: {
        proxyReq: (proxyReq, req, res) => {
          logger.debug("Proxying request to auth service", {
            path: req.originalUrl,
          });
        },
        error: (err, req, res) => {
          logger.error("Proxy error", {
            error: err.message || err || "Unknown error",
          });
          res.status(502).send("Bad Gateway");
        },
      },
    })
  );

  app.use(
    "/auth/refresh",
    createProxyMiddleware({
      target: `${authTarget}/auth/refresh`,
      changeOrigin: true,
      pathRewrite: { "^/auth/refresh": "" },
      on: {
        proxyReq: (proxyReq, req, res) => {
          logger.debug("Proxying request to auth service", {
            path: req.originalUrl,
          });
        },
        error: (err, req, res) => {
          logger.error("Proxy error", {
            error: err.message || err || "Unknown error",
          });
          res.status(502).send("Bad Gateway");
        },
      },
    })
  );

  app.use(
    "/auth/revalidate",
    createProxyMiddleware({
      target: `${authTarget}/auth/revalidate`,
      changeOrigin: true,
      pathRewrite: { "^/auth/revalidate": "" },
      on: {
        proxyReq: (proxyReq, req, res) => {
          logger.debug("Proxying request to auth service", {
            path: req.originalUrl,
          });
        },
        error: (err, req, res) => {
          logger.error("Proxy error", {
            error: err.message || err || "Unknown error",
          });
          res.status(502).send("Bad Gateway");
        },
      },
    })
  );
};
