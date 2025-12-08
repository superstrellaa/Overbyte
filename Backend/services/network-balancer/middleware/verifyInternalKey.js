module.exports = function verifyInternalKey(req, res, next) {
  const auth = req.headers.authorization;

  if (!auth || !auth.startsWith("Bearer "))
    return res.json({ code: "41", error: "Missing authorization" });

  const token = auth.split(" ")[1];

  if (token !== process.env.INTERNAL_API_KEY)
    return res.json({ code: "44", error: "Invalid internal API key" });

  next();
};
