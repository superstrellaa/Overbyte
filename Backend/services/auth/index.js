require("dotenv").config();
const express = require("express");
const cors = require("cors");
const rateLimit = require("express-rate-limit");
const bcrypt = require("bcrypt");
const jwt = require("jsonwebtoken");
const { v4: uuidv4 } = require("uuid");
const logger = require("@overbyte-backend/shared-logger");

const app = express();
const PORT = process.env.PORT || 3002;

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

const users = [];
const refreshTokens = new Map();

app.post("/auth/user", async (req, res) => {
  try {
    const { username, password } = req.body;

    if (!username || !password)
      return res.status(400).json({ error: "Username and password required" });

    const hashedPassword = await bcrypt.hash(password, 10);
    const newUser = {
      id: uuidv4(),
      username,
      password: hashedPassword,
      createdAt: new Date(),
    };

    users.push(newUser);

    logger.info("User registered", { username });
    res.status(201).json({ message: "User created successfully" });
  } catch (err) {
    logger.error("Registration error", { error: err.message });
    res.status(500).json({ error: "Server error" });
  }
});

app.post("/auth/login", async (req, res) => {
  try {
    const { username, password } = req.body;
    const user = users.find((u) => u.username === username);

    if (!user) return res.status(401).json({ error: "Invalid credentials" });

    const match = await bcrypt.compare(password, user.password);
    if (!match) return res.status(401).json({ error: "Invalid credentials" });

    const accessToken = jwt.sign(
      { id: user.id, username: user.username },
      process.env.ACCESS_TOKEN_SECRET,
      { expiresIn: "15m" }
    );

    const refreshToken = jwt.sign(
      { id: user.id },
      process.env.REFRESH_TOKEN_SECRET,
      { expiresIn: "7d" }
    );

    refreshTokens.set(refreshToken, user.id);

    res.json({ accessToken, refreshToken });
  } catch (err) {
    logger.error("Login error", { error: err.message });
    res.status(500).json({ error: "Server error" });
  }
});

app.post("/auth/refresh", (req, res) => {
  const { token } = req.body;
  if (!token || !refreshTokens.has(token))
    return res.status(403).json({ error: "Invalid refresh token" });

  try {
    const decoded = jwt.verify(token, process.env.REFRESH_TOKEN_SECRET);
    const accessToken = jwt.sign(
      { id: decoded.id },
      process.env.ACCESS_TOKEN_SECRET,
      { expiresIn: "15m" }
    );
    res.json({ accessToken });
  } catch (err) {
    logger.error("Refresh error", { error: err.message });
    res.status(403).json({ error: "Invalid refresh token" });
  }
});

app.listen(PORT, () => logger.info("Auth service listening", { port: PORT }));
