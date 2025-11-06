const Logger = require("../../utils/logger");

module.exports = {
  type: "changeGun",
  handler: (uuid, socket, message, roomId, { playerManager, roomManager }) => {
    const room = roomManager.getRoomByPlayer(uuid);
    if (!room || !room.players || !room.players.has(uuid)) {
      socket.send(
        JSON.stringify({
          type: "error",
          error: "Not in room. Join a room to change gun.",
        })
      );
      Logger.warn("changeGun denied: player not in room", { player: uuid });
      return;
    }
    const allowedGuns = [
      "Nothing",
      "HandGun",
      "Stinger",
      "Claw",
      "HandVulcan",
      "Ironfang",
      "Predator",
      "Executioner",
      "Bombard",
      "Vulcan",
    ];
    if (!allowedGuns.includes(message.gun)) {
      socket.send(
        JSON.stringify({
          type: "error",
          error: "Invalid gun type.",
        })
      );
      Logger.warn("changeGun denied: invalid gun", {
        player: uuid,
        gun: message.gun,
      });
      return;
    }
    roomManager.broadcastToRoom(uuid, {
      type: "gunChanged",
      uuid,
      gun: message.gun,
    });
  },
};
