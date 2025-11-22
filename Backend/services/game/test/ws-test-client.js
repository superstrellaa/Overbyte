const WebSocket = require("ws");

const ROOM_ID = process.argv[2] || "test-room";
const UUID = process.argv[3] || "test-" + Math.floor(Math.random() * 9999);
const HOST = "localhost";
const PORT = 3005;

const wsURL = `ws://${HOST}:${PORT}/?room=${ROOM_ID}&uuid=${UUID}`;

console.log("Connecting to:", wsURL);

const ws = new WebSocket(wsURL);

ws.on("open", () => {
  console.log("Connected as", UUID);

  // Envía un mensaje de test cada 3s
  setInterval(() => {
    ws.send(
      JSON.stringify({
        type: "move",
        x: Math.random() * 10,
        y: 0,
        z: Math.random() * 10,
        rotationY: Math.random() * 360,
        vx: (Math.random() - 0.5) * 10,
        vy: 0,
        vz: (Math.random() - 0.5) * 10,
      })
    );
  }, 2000);
});

ws.on("message", (raw) => {
  let msg = raw;
  try {
    msg = JSON.parse(raw);
  } catch {}

  console.log("Received:", msg);

  // Respuesta automática al heartbeat
  if (msg.type === "ping") {
    ws.send(JSON.stringify({ type: "pong" }));
  }
});

ws.on("close", () => {
  console.log("Disconnected.");
});

ws.on("error", (err) => {
  console.log("Error:", err.message);
});
