const WebSocket = require("ws");
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
    const uuid = url.searchParams.get("uuid");

    logger.info("WS incoming connection", { roomId, uuid });

    if (!roomId || !uuid) {
      ws.send(JSON.stringify({ error: "Missing room or uuid" }));
      return ws.close();
    }

    const room = roomManager.getRoom(roomId);

    if (!room) {
      ws.send(JSON.stringify({ error: "Room not found" }));
      logger.warn("WS room not found", { roomId, uuid });
      return ws.close();
    }

    room.addPlayer(uuid, ws);

    attachHeartbeat(ws, uuid, room);

    logger.info("WS player connected", { uuid, roomId });

    ws.send(JSON.stringify({ type: "joined", roomId, uuid }));

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

      handleClientMessage(room, uuid, msg);
    });

    ws.on("close", () => {
      room.removePlayer(uuid);
      if (ws._heartbeat) ws._heartbeat.stop();
      logger.info("WS player disconnected", { uuid, roomId });
    });
  });
}

module.exports = { initWebSocket, roomManager };
