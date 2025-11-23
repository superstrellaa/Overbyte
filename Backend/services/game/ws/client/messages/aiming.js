const logger = require("@overbyte-backend/shared-logger");

module.exports = (room, senderId, msg) => {
  if (typeof msg.pitch !== "number" || msg.pitch < -45 || msg.pitch > 45) {
    logger.warn("Aiming denied: invalid pitch", {
      player: senderId,
      pitch: msg.pitch,
      roomId: room.id,
    });

    room.sendTo(senderId, {
      type: "error",
      error: "Invalid pitch value.",
    });

    return;
  }

  const playerSlot = room.players.get(senderId);
  if (!playerSlot || !playerSlot.connected) {
    logger.warn("Aiming denied: player not in room", {
      player: senderId,
      roomId: room.id,
    });

    room.sendTo(senderId, {
      type: "error",
      error: "Not in room. Join a room to aim.",
    });

    return;
  }

  room.broadcastExcept(senderId, {
    type: "aiming",
    uuid: senderId,
    pitch: msg.pitch,
  });
};
