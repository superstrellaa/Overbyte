const logger = require("@overbyte-backend/shared-logger");
const fs = require("fs");
const path = require("path");

let colliders = null;
function getColliders() {
  if (!colliders) {
    const filePath = path.join(
      __dirname,
      "../../../config/colliders/colliders.json"
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

module.exports = (room, senderId, msg) => {
  if (!room || !room.players || !room.players.has(senderId)) return;

  const gunsConfig = require(".../../../config/guns");

  const gunName = msg.gun || "HandGun";
  const gun = gunsConfig[gunName] || gunsConfig["HandGun"];
  const maxDistance = gun.distance || 100;
  const damage = gun.damage || 25;

  let hitPoint = null;
  let origin = null;
  let hitNormal = null;

  if (msg.hit === "player" || msg.hit === "wall") {
    hitPoint = msg.hitPoint;
    origin = msg.origin;
    hitNormal = msg.hitNormal;

    const validVec = (v) =>
      v &&
      typeof v.x === "number" &&
      typeof v.y === "number" &&
      typeof v.z === "number";

    if (!validVec(hitPoint) || !validVec(origin) || !validVec(hitNormal)) {
      logger.warn("Invalid hitPoint/origin/hitNormal in shoot", {
        player: senderId,
        msg,
      });
      return;
    }
  }

  room.broadcastExcept(senderId, {
    type: "shootFired",
    uuid: senderId,
    hit: msg.hit,
    hitPoint,
    hitNormal,
    gun: gunName,
  });

  if (msg.hit !== "player" && msg.hit !== "wall") return;

  const collidersList = getColliders();

  const normalize = (v) => {
    const len = Math.sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    return len === 0
      ? { x: 0, y: 0, z: 0 }
      : { x: v.x / len, y: v.y / len, z: v.z / len };
  };

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
    logger.info("Shot too far", {
      player: senderId,
      gun: gunName,
      hitDistance,
      maxDistance,
    });
    return;
  }

  for (const collider of collidersList) {
    if (collider.type.toLowerCase() === "box") {
      if (rayIntersectsOBB(origin, dir, collider, hitDistance)) {
        logger.info("Shot blocked by wall", { player: senderId });
        return;
      }
    }
  }

  if (msg.hit === "player" && msg.hitUuid) {
    const targetSlot = room.players.get(msg.hitUuid);
    if (!targetSlot || !targetSlot.ws) return;

    if (typeof targetSlot.HP !== "number") targetSlot.HP = 100;
    targetSlot.HP = Math.max(0, targetSlot.HP - damage);

    room.sendTo(msg.hitUuid, {
      type: "shootReceived",
      HP: targetSlot.HP,
    });

    room.sendTo(senderId, {
      type: "shootGiven",
      targetUuid: msg.hitUuid,
      HP: damage,
    });

    logger.info("Player hit", {
      shooter: senderId,
      target: msg.hitUuid,
      newHP: targetSlot.HP,
      gun: gunName,
      damage,
    });
  }
};
