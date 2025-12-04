const logger = require("@overbyte-backend/shared-logger");

module.exports = (req, res, next) => {
  logger.info("Incoming request to auth-service", {
    method: req.method,
    path: req.originalUrl,
    ip: req.ip,
  });
  next();
};
