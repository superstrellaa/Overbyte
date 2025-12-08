const logger = require("@overbyte-backend/shared-logger");

const gameServers = new Map();

function registerServer(id, host, port, status = "online") {
  if (gameServers.has(id)) return false;

  gameServers.set(id, {
    id,
    host,
    port,
    players: 0,
    status,
    lastPing: Date.now(),
  });

  return true;
}

function updateHeartbeat(id, players, status = "online") {
  const gs = gameServers.get(id);
  if (!gs) return false;

  gs.players = players;
  gs.status = status;
  gs.lastPing = Date.now();
  return true;
}

function assignServer() {
  const available = [...gameServers.values()].filter(
    (gs) => gs.status === "online"
  );

  if (available.length === 0) return null;

  return available.sort((a, b) => a.players - b.players)[0];
}

function cleanupDeadServers() {
  const now = Date.now();
  for (const [id, gs] of gameServers.entries()) {
    if (now - gs.lastPing > 20000) {
      logger.warn("Removing dead game server", { id });
      gameServers.delete(id);
    }
  }
}

module.exports = {
  registerServer,
  updateHeartbeat,
  assignServer,
  cleanupDeadServers,
};
