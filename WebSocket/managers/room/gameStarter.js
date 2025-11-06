const matchmaking = require("../matchmaking");
const logger = require("../../utils/logger");
const mapManager = require("../mapManager");
const { shuffleArray } = require("../../utils/shuffle");

function sendStartGame(roomId, quantity = 1) {
  const queueObj = global.matchmakingQueues
    ? global.matchmakingQueues[quantity]
    : null;
  let room = null;
  if (queueObj && queueObj.rooms.has(roomId)) {
    room = queueObj.rooms.get(roomId);
  } else {
    room = matchmaking.rooms.get(roomId);
  }
  if (!room) return;

  const players = Array.from(room.players);

  const mapList = mapManager.getAllMaps();
  const validMap = mapList[0];
  const modeKey = `${quantity}v${quantity}`;
  const spawns = validMap.spawns[modeKey];
  if (!spawns || spawns.length < quantity * 2) {
    logger.error("No valid spawns for this mode", {
      context: "roomManager",
      playerCount: players.length,
      requiredSpawns: quantity * 2,
      modeKey,
    });
    return;
  }
  const shuffledSpawns = shuffleArray(spawns);
  const spawnAssignments = {};
  players.forEach((uuid, index) => {
    spawnAssignments[uuid] = shuffledSpawns[index];
  });

  const positions = {};
  players.forEach((uuid) => {
    positions[uuid] = spawnAssignments[uuid];
  });

  const playerManager = require("../../managers/playerManager");
  for (const playerUUID of room.players) {
    const player = playerManager.getPlayer(playerUUID);
    if (
      player &&
      player.socket &&
      player.socket.readyState === player.socket.OPEN
    ) {
      player.socket.send(
        JSON.stringify({
          type: "startGame",
          roomId,
          players,
          map: validMap.name,
        })
      );
      player.socket.send(
        JSON.stringify({
          type: "startPositions",
          positions,
        })
      );
    }
  }

  room.metadata = {
    map: validMap.name,
    spawnAssignments,
  };

  logger.info("Game started", {
    context: "roomManager",
    roomId,
    map: validMap.name,
    players,
    spawnAssignments,
  });
}

module.exports = { sendStartGame };
