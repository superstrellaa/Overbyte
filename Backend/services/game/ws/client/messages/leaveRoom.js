const logger = require("@overbyte-backend/shared-logger");

module.exports = (room, senderId, msg) => {
  if (!room || !room.players || !room.players.has(senderId)) {
    room.sendTo(senderId, {
      type: "error",
      error: "Not in any room",
    });
    return;
  }

  room.sendTo(senderId, {
    type: "roomLefted",
    roomId: room.id,
  });

  logger.info("Player left room", {
    player: senderId,
    context: "leaveRoom",
    roomId: room.id,
  });

  room.removePlayer(senderId);
};
