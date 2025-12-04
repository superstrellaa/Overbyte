require("dotenv").config();
const express = require("express");
const cors = require("cors");
const rateLimit = require("express-rate-limit");
const logger = require("@overbyte-backend/shared-logger");

const app = express();
const PORT = process.env.PORT || 3000;

app.use(
  cors({
    origin: process.env.CORS_ORIGIN || "*",
    methods: ["GET", "POST", "PUT", "DELETE"],
    allowedHeaders: ["Content-Type", "Authorization"],
  })
);

const limiter = rateLimit({
  windowMs: 60 * 1000,
  max: 60,
  message: { error: "Too many requests, slow down!" },
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

app.get("/", (req, res) => {
  res.json({ message: "Overbyte Gateway API" });
});

require("./routes/authProxy")(app);
require("./routes/versionProxy")(app);

app.listen(PORT, () => logger.info("Gateway listening", { port: PORT }));
