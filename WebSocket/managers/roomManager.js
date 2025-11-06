const { addPlayerToRoom } = require("./room/playerRoomAssigner");
const { broadcastToRoom } = require("./room/roomBroadcaster");
const { sendStartGame } = require("./room/gameStarter");
const { startRoomCleanup } = require("./room/roomCleaner");
const matchmaking = require("./matchmaking");

module.exports = {
  addPlayerToRoom,
  removePlayerFromRoom: function (uuid) {
    // Remove from all queues and rooms
    if (global.matchmakingQueues) {
      for (const q of Object.values(global.matchmakingQueues)) {
        // Remove from queue
        const idx = q.queue.indexOf(uuid);
        if (idx !== -1) q.queue.splice(idx, 1);
        // Remove from rooms
        for (const [roomId, room] of q.rooms.entries()) {
          if (room.players.has(uuid)) {
            room.players.delete(uuid);
            // If room is empty, delete it
            if (room.players.size === 0) q.rooms.delete(roomId);
          }
        }
      }
    }
    // Remove from legacy queue/room if present
    if (matchmaking.removePlayerFromQueue)
      matchmaking.removePlayerFromQueue(uuid);
  },
  broadcastToRoom,
  getRoomByPlayer: matchmaking.getRoomByPlayer,
  startRoomCleanup,
  sendStartGame,
};
