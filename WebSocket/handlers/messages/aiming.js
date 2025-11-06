const Logger = require("../../utils/logger");

module.exports = {
  type: "aiming",
  handler: (uuid, socket, message, roomId, { playerManager, roomManager }) => {
    const room = roomManager.getRoomByPlayer(uuid);
    if (!room || !room.players || !room.players.has(uuid)) {
      socket.send(
        JSON.stringify({
          type: "error",
          error: "Not in room. Join a room to aim.",
        })
      );
      Logger.warn("aiming denied: player not in room", { player: uuid });
      return;
    }
    if (
      typeof message.pitch !== "number" ||
      message.pitch < -45 ||
      message.pitch > 45
    ) {
      socket.send(
        JSON.stringify({
          type: "error",
          error: "Invalid pitch value.",
        })
      );
      Logger.warn("aiming denied: invalid pitch", {
        player: uuid,
        pitch: message.pitch,
      });
      return;
    }
    roomManager.broadcastToRoom(uuid, {
      type: "aiming",
      uuid,
      pitch: message.pitch,
    });
  },
};
