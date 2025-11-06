const logger = require("../../utils/logger");
const fs = require("fs");
const path = require("path");

let colliders = null;
function getColliders() {
  if (!colliders) {
    const filePath = path.join(
      __dirname,
      "../../config/collider/ServerCollisions.json"
    );
    colliders = JSON.parse(fs.readFileSync(filePath, "utf8")).colliders;
  }
  return colliders;
}

function rayIntersectsOBB(rayOrigin, rayDir, box, hitDistance) {
  const deg2rad = Math.PI / 180;
  const rotY = box.rotation?.y ? box.rotation.y * deg2rad : 0;

  const cosY = Math.cos(rotY);
  const sinY = Math.sin(rotY);

  const halfSize = {
    x: box.size.x / 2,
    y: box.size.y / 2,
    z: box.size.z / 2,
  };

  const dx = rayOrigin.x - box.position.x;
  const dz = rayOrigin.z - box.position.z;

  const localOrigin = {
    x: dx * cosY + dz * sinY,
    y: rayOrigin.y - box.position.y,
    z: -dx * sinY + dz * cosY,
  };

  const localDir = {
    x: rayDir.x * cosY + rayDir.z * sinY,
    y: rayDir.y,
    z: -rayDir.x * sinY + rayDir.z * cosY,
  };

  let tmin = -Infinity;
  let tmax = Infinity;

  for (const axis of ["x", "y", "z"]) {
    const invD = 1 / localDir[axis];
    const min = -halfSize[axis];
    const max = halfSize[axis];

    let t1 = (min - localOrigin[axis]) * invD;
    let t2 = (max - localOrigin[axis]) * invD;

    if (t1 > t2) [t1, t2] = [t2, t1];

    tmin = Math.max(tmin, t1);
    tmax = Math.min(tmax, t2);

    if (tmax < tmin) return false;
  }

  return tmin >= 0 && tmin <= hitDistance;
}

module.exports = {
  type: "shoot",
  handler: (uuid, socket, message, roomId, { playerManager, roomManager }) => {
    const room = roomManager.getRoomByPlayer(uuid);
    if (!room || !room.players || !room.players.has(uuid)) return;

    const gunsConfig = require("../../config/guns");
    const gunName = message.gun || "HandGun";
    const gun = gunsConfig[gunName] || gunsConfig["HandGun"];
    const maxDistance = gun.distance || 100;
    const damage = gun.damage || 25;

    let hitPoint = null;
    let origin = null;
    let hitNormal = null;
    if (message.hit === "player" || message.hit === "wall") {
      hitPoint = message.hitPoint;
      origin = message.origin;
      hitNormal = message.hitNormal;
      if (
        !hitPoint ||
        typeof hitPoint.x !== "number" ||
        typeof hitPoint.y !== "number" ||
        typeof hitPoint.z !== "number" ||
        !origin ||
        typeof origin.x !== "number" ||
        typeof origin.y !== "number" ||
        typeof origin.z !== "number" ||
        !hitNormal ||
        typeof hitNormal.x !== "number" ||
        typeof hitNormal.y !== "number" ||
        typeof hitNormal.z !== "number"
      ) {
        logger.warn("Invalid hitPoint, origin or hitNormal in shoot message", {
          player: uuid,
          message,
        });
        return;
      }
    }

    roomManager.broadcastToRoom(uuid, {
      type: "shootFired",
      uuid,
      hit: message.hit,
      hitPoint,
      hitNormal,
      gun: gunName,
    });

    if (message.hit === "player" || message.hit === "wall") {
      const collidersList = getColliders();
      function normalize(v) {
        const len = Math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        return len === 0
          ? { x: 0, y: 0, z: 0 }
          : { x: v.x / len, y: v.y / len, z: v.z / len };
      }

      const dir = normalize({
        x: hitPoint.x - origin.x,
        y: hitPoint.y - origin.y,
        z: hitPoint.z - origin.z,
      });

      const hitDistance = Math.sqrt(
        (hitPoint.x - origin.x) ** 2 +
          (hitPoint.y - origin.y) ** 2 +
          (hitPoint.z - origin.z) ** 2
      );

      if (hitDistance > maxDistance) {
        logger.info("Shot too far for weapon", {
          player: uuid,
          gun: gunName,
          hitDistance,
          maxDistance,
        });
        return;
      }

      for (const collider of collidersList) {
        if (collider.type.toLowerCase() === "box") {
          if (rayIntersectsOBB(origin, dir, collider, hitDistance)) {
            logger.info("Shot blocked by wall between origin and hitPoint", {
              player: uuid,
            });
            return;
          }
        }
      }
    }

    if (message.hit === "player" && message.hitUuid) {
      const target = playerManager.getPlayer(message.hitUuid);
      if (target && typeof target.HP === "number") {
        target.HP = Math.max(0, target.HP - damage);

        if (target.socket && target.socket.readyState === target.socket.OPEN) {
          target.socket.send(
            JSON.stringify({
              type: "shootReceived",
              HP: target.HP,
            })
          );
        }

        if (socket && socket.readyState === socket.OPEN) {
          socket.send(
            JSON.stringify({
              type: "shootGiven",
              targetUuid: message.hitUuid,
              HP: damage,
            })
          );
        }

        logger.info("Player hit", {
          shooter: uuid,
          target: message.hitUuid,
          targetHP: target.HP,
          hitPoint,
          gun: gunName,
          damage,
        });
      }
    }
  },
};
