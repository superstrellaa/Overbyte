const jwt = require("jsonwebtoken");
const prisma = require("../database/client");

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
    data: { token, userId, expiresAt },
  });

  return token;
}

module.exports = {
  generateAccessToken,
  generateRefreshToken,
};
