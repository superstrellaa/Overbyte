const WebSocket = require("ws");
const jwt = require("jsonwebtoken");
const logger = require("@overbyte-backend/shared-logger");

const RoomManager = require("./room/RoomManager");
const { handleClientMessage } = require("./client/messageHandler");
const { attachHeartbeat } = require("./heartbeat");

const roomManager = new RoomManager();

function initWebSocket(server) {
  const wss = new WebSocket.Server({ server });

  wss.on("connection", (ws, req) => {
    const url = new URL(req.url, `http://${req.headers.host}`);

    const roomId = url.searchParams.get("room");
    const accessToken = url.searchParams.get("accessToken");

    if (!roomId || !accessToken) {
      ws.send(JSON.stringify({ error: "Missing room or accessToken" }));
      return ws.close();
    }

    let userId;

    try {
      const decoded = jwt.verify(accessToken, process.env.ACCESS_TOKEN_SECRET);
      userId = decoded.userId;
    } catch (err) {
      ws.send(JSON.stringify({ error: "Invalid or expired accessToken" }));
      logger.warn("WS invalid access token", { reason: err.message });
      return ws.close();
    }

    logger.info("WS incoming connection", { roomId, userId });

    const room = roomManager.getRoom(roomId);

    if (!room) {
      ws.send(JSON.stringify({ error: "Room not found" }));
      logger.warn("WS room not found", { roomId, userId });
      return ws.close();
    }

    room.addPlayer(userId, ws);
    attachHeartbeat(ws, userId, room);

    logger.info("WS player connected", { userId, roomId });

    ws.send(JSON.stringify({ type: "joined", roomId, userId }));

    ws.on("message", (raw) => {
      let msg;
      try {
        msg = JSON.parse(raw);
      } catch {
        return;
      }

      if (msg.type === "pong") {
        if (ws._heartbeat) ws._heartbeat.onPong();
        return;
      }

      handleClientMessage(room, userId, msg);
    });

    ws.on("close", () => {
      room.removePlayer(userId);

      if (ws._heartbeat) ws._heartbeat.stop();
      logger.info("WS player disconnected", { userId, roomId });
    });

    ws.on("error", (err) => {
      logger.error("WS socket error", {
        error: err.message,
        roomId,
        userId,
      });
    });
  });
}

module.exports = { initWebSocket, roomManager };
