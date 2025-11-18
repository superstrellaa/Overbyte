const logger = require("@overbyte-backend/shared-logger");

const {
  SERVER_PING_INTERVAL_MS,
  MAX_MISSED_PONGS,
} = require("../config/server");

function attachHeartbeat(ws, uuid, room) {
  let missed = 0;

  const interval = setInterval(() => {
    if (missed >= MAX_MISSED_PONGS) {
      logger.warn("Client missed too many pongs, closing connection", {
        uuid,
        roomId: room.roomId,
      });

      clearInterval(interval);
      ws.terminate();
      return;
    }

    try {
      ws.send(JSON.stringify({ type: "ping" }));
      missed++;
    } catch (err) {
      logger.error("Failed to send ping", { uuid, error: err.message });
      missed++;
    }
  }, SERVER_PING_INTERVAL_MS);

  ws._heartbeat = {
    onPong: () => {
      missed = 0;
    },
    stop: () => clearInterval(interval),
  };
}

module.exports = { attachHeartbeat };
