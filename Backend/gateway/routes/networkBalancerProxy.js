const { createProxyMiddleware } = require("http-proxy-middleware");
const logger = require("@overbyte-backend/shared-logger");
const { patch } = require("../../services/auth/routes/auth.routes");

module.exports = (app) => {
  const networkBalancerTarget =
    process.env.NODE_ENV === "production"
      ? `http://${process.env.NETWORK_BALANCER_HOST}:${process.env.NETWORK_BALANCER_PORT}`
      : `http://localhost:3006`;

  app.use(
    "/network/register",
    createProxyMiddleware({
      target: `${networkBalancerTarget}/network/register`,
      changeOrigin: true,
      pathRewrite: { "^/network/register": "" },
      on: {
        proxyReq: (proxyReq, req, res) => {
          logger.debug("Proxying request to network balancer service", {
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

  app.use(
    "/network/heartbeat",
    createProxyMiddleware({
      target: `${networkBalancerTarget}/network/heartbeat`,
      changeOrigin: true,
      pathRewrite: { "^/network/heartbeat": "" },
      on: {
        proxyReq: (proxyReq, req, res) => {
          logger.debug("Proxying request to network balancer service", {
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

  app.use(
    "/network/assign-server",
    createProxyMiddleware({
      target: `${networkBalancerTarget}/network/assign-server`,
      changeOrigin: true,
      pathRewrite: { "^/network/assign-server": "" },
      on: {
        proxyReq: (proxyReq, req, res) => {
          logger.debug("Proxying request to network balancer service", {
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
