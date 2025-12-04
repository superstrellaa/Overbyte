require("dotenv").config();
const express = require("express");
const logger = require("@overbyte-backend/shared-logger");

const corsMiddleware = require("./middleware/cors");
const rateLimiter = require("./middleware/rateLimit");
const logging = require("./middleware/logging");

const authRoutes = require("./routes/auth.routes");

const app = express();
const PORT = process.env.PORT || 3002;

app.use(express.json());
app.use(corsMiddleware);
app.use(rateLimiter);
app.use(logging);

app.use("/auth", authRoutes);

app.listen(PORT, () => logger.info("Auth service listening", { port: PORT }));
