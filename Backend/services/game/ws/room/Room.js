const WebSocket = require("ws");
const maps = require("../../config/maps");
const logger = require("@overbyte-backend/shared-logger");

class Room {
  constructor(id, options = {}) {
    this.id = id;
    this.mode = options.mode || "default";
    this.maxPlayers = options.maxPlayers || 2;

    this.players = new Map();
    this.createdAt = Date.now();
    this.status = "waiting";

    this.timeoutMs = options.timeoutMs || 15000;
    this.timeoutHandler = null;

    this.onCancel = null;
    this.onEmpty = null;

    this.startCountdown();
  }

  startCountdown() {
    this.clearCountdown();
    this.timeoutHandler = setTimeout(() => {
      const connected = this.playersConnectedCount();

      if (connected !== this.maxPlayers && this.status === "waiting") {
        this.status = "cancelled";

        for (const [uuid, slot] of this.players.entries()) {
          try {
            if (slot.ws && slot.ws.readyState === WebSocket.OPEN) {
              slot.ws.send(
                JSON.stringify({
                  type: "roomCancelled",
                  reason: "not_enough_players",
                })
              );
              slot.ws.close();
            }
          } catch (e) {
            logger.warn("Error closing player socket during room cancel", {
              roomId: this.id,
              uuid,
              error: e.message,
            });
          }
        }

        logger.info("Room cancelled due to timeout", {
          roomId: this.id,
          expected: this.maxPlayers,
          connected,
        });

        if (typeof this.onCancel === "function") this.onCancel(this.id);
      }
    }, this.timeoutMs);
  }

  clearCountdown() {
    if (this.timeoutHandler) {
      clearTimeout(this.timeoutHandler);
      this.timeoutHandler = null;
    }
  }

  addPlayer(uuid, ws) {
    if (this.status !== "waiting") {
      try {
        ws.send(
          JSON.stringify({ type: "error", error: "game_already_started" })
        );
      } catch (e) {}
      return ws.close();
    }

    this.players.set(uuid, { ws, connected: true });
    global.__CURRENT_PLAYERS__ = (global.__CURRENT_PLAYERS__ || 0) + 1;

    const connected = this.playersConnectedCount();
    const allConnected = connected === this.maxPlayers;

    if (allConnected && this.status === "waiting") {
      this.clearCountdown();

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

  removePlayer(uuid) {
    const slot = this.players.get(uuid);
    if (slot) {
      slot.connected = false;
      try {
        if (slot.ws && slot.ws.readyState === WebSocket.OPEN) {
          slot.ws.send(
            JSON.stringify({ type: "roomDeleted", roomId: this.id })
          );
          slot.ws.close();
        }
      } catch (e) {
        logger.warn("Error notifying player on remove", {
          roomId: this.id,
          uuid,
          error: e.message,
        });
      }
    }

    this.players.delete(uuid);

    if (this.players.size === 0) {
      logger.info("Room empty, scheduling deletion", { roomId: this.id });

      this.clearCountdown();

      if (typeof this.onEmpty === "function") this.onEmpty(this.id);
      return;
    }

    this.broadcast({
      type: "playerDisconnected",
      uuid,
    });
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

  broadcast(payload) {
    const msg = JSON.stringify(payload);
    for (const { ws } of this.players.values()) {
      try {
        if (ws && ws.readyState === WebSocket.OPEN) {
          ws.send(msg);
        }
      } catch (e) {
        logger.warn("Failed sending broadcast to a player", {
          roomId: this.id,
          error: e.message,
        });
      }
    }
  }

  sendTo(uuid, payload) {
    const slot = this.players.get(uuid);
    if (!slot || !slot.ws) return;
    try {
      if (slot.ws.readyState === WebSocket.OPEN) {
        slot.ws.send(JSON.stringify(payload));
      }
    } catch (e) {
      logger.warn("Failed sending to player", {
        roomId: this.id,
        uuid,
        error: e.message,
      });
    }
  }

  broadcastExcept(uuid, payload) {
    const msg = JSON.stringify(payload);
    for (const [id, { ws }] of this.players.entries()) {
      if (id === uuid) continue;
      try {
        if (ws && ws.readyState === WebSocket.OPEN) ws.send(msg);
      } catch (e) {
        logger.warn("Failed sending broadcastExcept to a player", {
          roomId: this.id,
          to: id,
          error: e.message,
        });
      }
    }
  }
}

module.exports = Room;
