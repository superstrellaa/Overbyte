const prisma = require("../database/client");
const jwt = require("jsonwebtoken");
const logger = require("@overbyte-backend/shared-logger");
const validateInternalApiKey = require("../middleware/validateInternalKey");
const {
  generateAccessToken,
  generateRefreshToken,
} = require("../utils/tokens");
const { nanoid } = require("nanoid");

module.exports = {
  registerOrLogin: async (req, res) => {
    if (!validateInternalApiKey(req, res)) return;

    const { username } = req.body;
    if (!username) return res.status(400).json({ error: "Username required" });

    let user = await prisma.user.findUnique({ where: { username } });

    if (!user) {
      user = await prisma.user.create({
        data: { userId: nanoid(10), username },
      });

      logger.info("User registered", { userId: user.userId, username });
    }

    const accessToken = generateAccessToken(user.userId);
    const refreshToken = await generateRefreshToken(user.userId);

    logger.info("User logged in", { userId: user.userId });
    res.json({ userId: user.userId, accessToken, refreshToken });
  },

  refresh: async (req, res) => {
    const { refreshToken } = req.body;
    if (!refreshToken)
      return res.status(403).json({ error: "Missing refresh token" });

    const dbToken = await prisma.refreshToken.findUnique({
      where: { token: refreshToken },
    });

    if (!dbToken)
      return res.status(403).json({ error: "Invalid refresh token" });

    if (dbToken.expiresAt < new Date()) {
      await prisma.refreshToken.delete({ where: { token: refreshToken } });
      return res.status(403).json({ error: "Expired refresh token" });
    }

    try {
      const decoded = jwt.verify(
        refreshToken,
        process.env.REFRESH_TOKEN_SECRET
      );

      const newAccessToken = generateAccessToken(decoded.userId);
      const newRefreshToken = await generateRefreshToken(decoded.userId);

      await prisma.refreshToken.delete({ where: { token: refreshToken } });

      logger.info("Refresh token rotated", { userId: decoded.userId });

      res.json({ accessToken: newAccessToken, refreshToken: newRefreshToken });
    } catch (err) {
      logger.error("Refresh error", { error: err.message });
      res.status(403).json({ error: "Invalid refresh token" });
    }
  },

  revalidate: async (req, res) => {
    if (!validateInternalApiKey(req, res)) return;

    const { userId } = req.body;
    if (!userId) return res.status(400).json({ error: "Missing userId" });

    const user = await prisma.user.findUnique({ where: { userId } });
    if (!user) return res.status(404).json({ error: "User not found" });

    const accessToken = generateAccessToken(userId);
    const refreshToken = await generateRefreshToken(userId);

    logger.info("User revalidated", { userId });

    res.json({ accessToken, refreshToken });
  },
};
