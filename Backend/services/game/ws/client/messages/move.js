const antiCheat = require("../../../utils/antiCheat");
const logger = require("@overbyte-backend/shared-logger");

module.exports = (room, senderId, msg) => {
  const suspicious = antiCheat.isMoveSuspicious(senderId, {
    x: msg.x,
    y: msg.y,
    z: msg.z,
    rotationY: msg.rotationY,
    vx: msg.vx,
    vy: msg.vy,
    vz: msg.vz,
  });

  if (suspicious) {
    logger.warn("[AntiCheat] Suspicious move detected, move cancelled", {
      player: senderId,
      context: "move",
      roomId: room.id,
      ...msg,
    });

    room.sendTo(senderId, {
      type: "error",
      error: "Suspicious movement detected. Move cancelled.",
    });

    return;
  }

  // logger.info("Player moved", {
  //   player: senderId,
  //   context: "move",
  //   roomId: room.id,
  //   ...msg,
  // });

  room.broadcastExcept(senderId, {
    type: "playerMoved",
    uuid: senderId,
    x: msg.x,
    y: msg.y,
    z: msg.z,
    rotationY: msg.rotationY,
    vx: msg.vx,
    vy: msg.vy,
    vz: msg.vz,
  });
};
