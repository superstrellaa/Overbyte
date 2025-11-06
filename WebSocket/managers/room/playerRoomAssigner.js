const matchmaking = require("../matchmaking");
const ConfigManager = require("../configManager");
const { sendStartGame } = require("./gameStarter");
const UNLIMITED_ROOM_ID = "unlimited-room";

function cleanupUnlimitedRoomIfNeeded() {
  if (!ConfigManager.game.roomUnlimited) {
    const unlimitedRoom = matchmaking.rooms.get(UNLIMITED_ROOM_ID);
    if (unlimitedRoom) {
      for (const uuid of unlimitedRoom.players) {
        matchmaking.removePlayerFromRoomMap(uuid);
      }
      matchmaking.rooms.delete(UNLIMITED_ROOM_ID);
    }
  }
}

cleanupUnlimitedRoomIfNeeded();

function addPlayerToRoom(uuid) {
  const playerManager = require("../../managers/playerManager");
  let quantity = 1;
  if (arguments.length > 1) {
    quantity = Math.max(1, Math.min(4, Number(arguments[1]) || 1));
  }
  if (!global.matchmakingQueues) {
    global.matchmakingQueues = {
      1: { queue: [], rooms: new Map() },
      2: { queue: [], rooms: new Map() },
      3: { queue: [], rooms: new Map() },
      4: { queue: [], rooms: new Map() },
    };
  }
  const queueObj = global.matchmakingQueues[quantity];
  if (queueObj.queue.includes(uuid)) return null;
  queueObj.queue.push(uuid);
  if (queueObj.queue.length >= quantity * 2) {
    const roomId = `${quantity}v${quantity}-room-${Date.now()}-${Math.floor(
      Math.random() * 10000
    )}`;
    const roomPlayers = queueObj.queue.splice(0, quantity * 2);
    const Room = require("../../models/Room");
    const room = new Room(roomId);
    roomPlayers.forEach((p) => room.addPlayer(p));
    queueObj.rooms.set(roomId, room);
    roomPlayers.forEach((p) => matchmaking.assignPlayerToRoom(p, roomId));
    roomPlayers.forEach((p) => {
      const player = playerManager.getPlayer(p);
      if (player) player.roomId = roomId;
    });
    sendStartGame(roomId, quantity);
    return roomId;
  }
  return null;
}

module.exports = { addPlayerToRoom };
