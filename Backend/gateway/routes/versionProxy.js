const { createProxyMiddleware } = require("http-proxy-middleware");
const logger = require("@overbyte-backend/shared-logger");

module.exports = (app) => {
  const versionTarget =
    process.env.NODE_ENV === "production"
      ? `http://${process.env.VERSION_SERVICE_HOST}:${process.env.VERSION_SERVICE_PORT}`
      : `http://localhost:3001`;

  app.use(
    "/game/version",
    createProxyMiddleware({
      target: `${versionTarget}/game/version`,
      changeOrigin: true,
      pathRewrite: { "^/game/version": "" },
      on: {
        proxyReq: (proxyReq, req, res) => {
          logger.debug("Proxying request to version service", {
            path: req.originalUrl,
          });
        },
        error: (err, req, res) => {
          logger.error("Proxy error", {
            error: err.message || err || "Unknown error",
          });
          res.json({
            code: "05",
            error: "Proxy error, service closed or unknown",
          });
        },
      },
    })
  );
};
