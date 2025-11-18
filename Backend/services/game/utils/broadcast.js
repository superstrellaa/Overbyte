function broadcastToMap(map, payload) {
  const msg = JSON.stringify(payload);
  for (const [, slot] of map.entries()) {
    if (slot.ws && slot.ws.readyState === 1) {
      slot.ws.send(msg);
    }
  }
}

module.exports = { broadcastToMap };
