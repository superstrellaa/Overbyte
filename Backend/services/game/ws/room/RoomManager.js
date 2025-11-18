const Room = require("./Room");

class RoomManager {
  constructor() {
    this.rooms = new Map();
  }

  createRoom(id, options) {
    const room = new Room(id, options);
    this.rooms.set(id, room);
    return room;
  }

  getRoom(id) {
    return this.rooms.get(id);
  }

  deleteRoom(id) {
    this.rooms.delete(id);
  }
}

module.exports = RoomManager;
