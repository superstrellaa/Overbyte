require("dotenv").config();
const express = require("express");
const cors = require("cors");
const rateLimit = require("express-rate-limit");
const jwt = require("jsonwebtoken");
const { nanoid } = require("nanoid");
const logger = require("@overbyte-backend/shared-logger");

// ------------ PRISMA NUEVO (ADAPTER-PG + OUTPUT CUSTOM) ------------
const { PrismaClient } = require("./generated/prisma");
const { PrismaPg } = require("@prisma/adapter-pg");

const adapter = new PrismaPg({
  connectionString: process.env.DATABASE_URL,
});

const prisma = new PrismaClient({ adapter });
// -------------------------------------------------------------------

const app = express();
const PORT = process.env.PORT || 3002;

// --------------------- Middleware ---------------------
app.use(express.json());
app.use(
  cors({
    origin: process.env.CORS_ORIGIN || "*",
    methods: ["GET", "POST"],
  })
);

app.use(
  rateLimit({
    windowMs: 60 * 1000,
    max: 100,
    message: { error: "Too many requests, slow down!" },
  })
);

app.use((req, res, next) => {
  logger.info("Incoming request to auth-service", {
    method: req.method,
    path: req.originalUrl,
    ip: req.ip,
  });
  next();
});

// --------------------- Helpers ---------------------
function generateAccessToken(userId) {
  return jwt.sign({ userId }, process.env.ACCESS_TOKEN_SECRET, {
    expiresIn: "15m",
  });
}

async function generateRefreshToken(userId) {
  const token = jwt.sign({ userId }, process.env.REFRESH_TOKEN_SECRET, {
    expiresIn: "7d",
  });

  const expiresAt = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);

  await prisma.refreshToken.create({
    data: {
      token,
      userId,
      expiresAt,
    },
  });

  return token;
}

function validateInternalApiKey(req, res) {
  const authHeader = req.headers.authorization;
  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    res.status(401).json({ error: "Missing authorization" });
    return false;
  }
  const token = authHeader.split(" ")[1];
  if (token !== process.env.INTERNAL_API_KEY) {
    res.status(403).json({ error: "Invalid internal API key" });
    return false;
  }
  return true;
}

// --------------------- Endpoints ---------------------

// POST /auth/user → registro o primer login
app.post("/auth/user", async (req, res) => {
  if (!validateInternalApiKey(req, res)) return;

  const { username } = req.body;
  if (!username) return res.status(400).json({ error: "Username required" });

  let user = await prisma.user.findUnique({ where: { username } });

  if (!user) {
    user = await prisma.user.create({
      data: {
        userId: nanoid(10),
        username,
      },
    });

    logger.info("User registered", { userId: user.userId, username });
  }

  const accessToken = generateAccessToken(user.userId);
  const refreshToken = await generateRefreshToken(user.userId);

  res.json({ userId: user.userId, accessToken, refreshToken });
});

// POST /auth/refresh → usar refresh token rotacional
app.post("/auth/refresh", async (req, res) => {
  const { refreshToken } = req.body;

  if (!refreshToken)
    return res.status(403).json({ error: "Missing refresh token" });

  const dbToken = await prisma.refreshToken.findUnique({
    where: { token: refreshToken },
  });

  if (!dbToken) return res.status(403).json({ error: "Invalid refresh token" });

  if (dbToken.expiresAt < new Date()) {
    await prisma.refreshToken.delete({ where: { token: refreshToken } });
    return res.status(403).json({ error: "Expired refresh token" });
  }

  try {
    const decoded = jwt.verify(refreshToken, process.env.REFRESH_TOKEN_SECRET);

    const newAccessToken = generateAccessToken(decoded.userId);
    const newRefreshToken = await generateRefreshToken(decoded.userId);

    await prisma.refreshToken.delete({ where: { token: refreshToken } });

    logger.info("Refresh token rotated", { userId: decoded.userId });

    res.json({ accessToken: newAccessToken, refreshToken: newRefreshToken });
  } catch (err) {
    logger.error("Refresh error", { error: err.message });
    return res.status(403).json({ error: "Invalid refresh token" });
  }
});

// POST /auth/revalidate → refresh token caducado, usa internal API key
app.post("/auth/revalidate", async (req, res) => {
  if (!validateInternalApiKey(req, res)) return;

  const { userId } = req.body;
  if (!userId) return res.status(400).json({ error: "Missing userId" });

  const user = await prisma.user.findUnique({ where: { userId } });
  if (!user) return res.status(404).json({ error: "User not found" });

  const accessToken = generateAccessToken(userId);
  const refreshToken = await generateRefreshToken(userId);

  logger.info("User revalidated", { userId });

  res.json({ accessToken, refreshToken });
});

// --------------------- Start server ---------------------
app.listen(PORT, () => logger.info("Auth service listening", { port: PORT }));
