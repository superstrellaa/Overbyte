require("dotenv").config();
const express = require("express");
const cors = require("cors");
const rateLimit = require("express-rate-limit");
const networkRoutes = require("./routes/network");
const cleanup = require("./utils/cleanup");
const logger = require("@overbyte-backend/shared-logger");

const app = express();

app.use(express.json());

app.use(
  cors({
    origin: process.env.CORS_ORIGIN || "*",
    methods: ["GET", "POST"],
    allowedHeaders: ["Content-Type", "Authorization"],
  })
);

const limiter = rateLimit({
  windowMs: 60 * 1000,
  max: 60,
  message: { code: "43", error: "Too many requests, slow down!" },
});

app.use(limiter);

app.use((req, res, next) => {
  logger.info("Incoming request", {
    method: req.method,
    path: req.originalUrl,
    ip: req.ip,
  });
  next();
});

app.use("/network", networkRoutes);

cleanup();

module.exports = app;
