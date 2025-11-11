require("dotenv").config();
const express = require("express");
const cors = require("cors");
const rateLimit = require("express-rate-limit");
const logger = require("@overbyte-backend/shared-logger");

const app = express();
const PORT = process.env.PORT || 3001;

app.use(
  cors({
    origin: process.env.CORS_ORIGIN || "*",
    methods: ["GET"],
  })
);

app.use(
  rateLimit({
    windowMs: 60 * 1000,
    max: 100,
    message: { error: "Too many requests, slow down!" },
  })
);

app.use((req, res, next) => {
  logger.info("Incoming request to version-service", {
    method: req.method,
    path: req.originalUrl,
    ip: req.ip,
  });
  next();
});

app.get("/game/version", (req, res) => {
  const version = process.env.GAME_VERSION || "1.0.0";
  logger.info("Version requested", { version });
  res.json({ version });
});

app.listen(PORT, () =>
  logger.info("Version service listening", { port: PORT })
);
