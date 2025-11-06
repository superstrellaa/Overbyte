const Room = require("../../models/Room");

const roomState = {
  rooms: new Map(),
  playerToRoom: new Map(),
};

function getRoomByPlayer(uuid) {
  const { playerToRoom } = roomState;
  const roomId = playerToRoom.get(uuid);
  if (!roomId) return null;
  // Search all matchmakingQueues for the room
  if (global.matchmakingQueues) {
    for (const q of Object.values(global.matchmakingQueues)) {
      if (q.rooms.has(roomId)) return q.rooms.get(roomId);
    }
  }
  return roomState.rooms.get(roomId) || null;
}

function isPlayerInRoom(uuid) {
  const { playerToRoom, rooms } = roomState;
  const roomId = playerToRoom.get(uuid);
  if (!roomId) return false;
  const room = rooms.get(roomId);
  return !!(room && room.players.has(uuid));
}

function assignPlayerToRoom(uuid, roomId) {
  roomState.playerToRoom.set(uuid, roomId);
}

function removePlayerFromRoomMap(uuid) {
  roomState.playerToRoom.delete(uuid);
}

module.exports = {
  roomState,
  getRoomByPlayer,
  isPlayerInRoom,
  assignPlayerToRoom,
  removePlayerFromRoomMap,
};
