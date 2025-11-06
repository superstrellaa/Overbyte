const roomManager = require("../../managers/roomManager");
const Logger = require("../../utils/logger");

module.exports = {
  type: "joinQueue",
  handler: (uuid, socket, message, roomId, { playerManager }) => {
    const quantity = Math.max(1, Math.min(4, Number(message.quantity) || 1));
    const assignedRoomId = roomManager.addPlayerToRoom(uuid, quantity);
    Logger.info("Player joined matchmaking queue", {
      player: uuid,
      context: "joinQueue",
      assignedRoomId,
      quantity,
    });
  },
};
