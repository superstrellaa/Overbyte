const rateLimit = require("express-rate-limit");

module.exports = rateLimit({
  windowMs: 60 * 1000,
  max: 100,
  message: { code: "23", error: "Too many requests, slow down!" },
});
