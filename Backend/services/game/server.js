// server.js (opción B: HTTP y WS en puertos separados)
const express = require("express");
const cors = require("cors");
const rateLimit = require("express-rate-limit");
const http = require("http");
const logger = require("@overbyte-backend/shared-logger");

const registerHealthRoutes = require("./routes/health");
const registerRoomRoutes = require("./routes/createRoom");
const { initWebSocket } = require("./ws/websocket");

function startServer() {
  const app = express();
  app.use(express.json());

  app.use(cors({ origin: process.env.CORS_ORIGIN || "*" }));

  app.use(
    rateLimit({
      windowMs: 60 * 1000,
      max: 200,
      message: { error: "Too many requests" },
    })
  );

  app.use((req, res, next) => {
    logger.info("Incoming request", {
      method: req.method,
      path: req.originalUrl,
      ip: req.ip,
    });
    next();
  });

  registerHealthRoutes(app);
  registerRoomRoutes(app);

  const apiServer = http.createServer(app);
  const API_PORT = Number(process.env.PORT) || 3003;
  apiServer.listen(API_PORT, () => {
    logger.info("Game service API listening", { APIPort: API_PORT });
  });

  const WS_PORT = Number(process.env.GAME_WS_PORT) || API_PORT;
  const wsServer = http.createServer();
  initWebSocket(wsServer);
  wsServer.listen(WS_PORT, () => {
    logger.info("Game service WS listening", {
      WSHost: process.env.GAME_WS_HOST || "localhost",
      WSPort: WS_PORT,
    });
  });
}

module.exports = { startServer };
