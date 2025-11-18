const WebSocket = require("ws");
const maps = require("../../config/maps");

class Room {
  constructor(id, options = {}) {
    this.id = id;
    this.mode = options.mode || "default";

    this.maxPlayers = options.maxPlayers || 2;

    this.players = new Map();
    this.createdAt = Date.now();
    this.status = "waiting";
  }

  addPlayer(uuid, ws) {
    this.players.set(uuid, { ws, connected: true });

    const connected = this.playersConnectedCount();
    const allConnected = connected === this.maxPlayers;

    if (allConnected && this.status === "waiting") {
      this.status = "playing";

      this.mode = this.getModeByPlayers(this.maxPlayers);

      const map = this.pickRandomMap();

      const spawnList = map.spawns[this.mode];

      const positions = this.assignSpawns([...this.players.keys()], spawnList);

      this.broadcast({
        type: "startGame",
        roomId: this.id,
        players: [...this.players.keys()],
        map: map.name,
      });

      this.broadcast({
        type: "startPositions",
        positions,
      });
    }
  }

  getModeByPlayers(count) {
    switch (count) {
      case 2:
        return "1v1";
      case 4:
        return "2v2";
      case 6:
        return "3v3";
      case 8:
        return "4v4";
      default:
        return `${count}v${count}`;
    }
  }

  pickRandomMap() {
    const index = Math.floor(Math.random() * maps.length);
    return maps[index];
  }

  assignSpawns(uuids, spawns) {
    const positions = {};

    uuids.forEach((uuid, i) => {
      positions[uuid] = spawns[i];
    });

    return positions;
  }

  playersConnectedCount() {
    return [...this.players.values()].filter((p) => p.connected).length;
  }

  removePlayer(uuid) {
    const slot = this.players.get(uuid);
    if (slot) slot.connected = false;
  }

  broadcast(payload) {
    const msg = JSON.stringify(payload);
    for (const { ws } of this.players.values()) {
      if (ws && ws.readyState === WebSocket.OPEN) {
        ws.send(msg);
      }
    }
  }

  sendTo(uuid, payload) {
    const slot = this.players.get(uuid);
    if (!slot || !slot.ws) return;
    if (slot.ws.readyState === WebSocket.OPEN) {
      slot.ws.send(JSON.stringify(payload));
    }
  }

  broadcastExcept(uuid, payload) {
    const msg = JSON.stringify(payload);
    for (const [id, { ws }] of this.players.entries()) {
      if (id === uuid) continue;
      if (ws && ws.readyState === WebSocket.OPEN) ws.send(msg);
    }
  }
}

module.exports = Room;
