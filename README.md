# Overbyte Project

# If you are here because you see that this repository is open-source, I will explain you the folder's index:
- The real backend that I worked the last time of my live but that isn't finished is the `Backend/` folder
- Then, the other's `WebSocket/` and `API/` folders are the old backend project that works but... potato
- And the unity project **6.0** is the main `Overbyte/` folder, the Unity Project it has not been updated to the new backend, so if you want to test it you will be use the `WebSocket/` and `API/` project

## This project do __not have license__, so you are free to copy, sell, create your own project, etc...

# Credits:
### The main part of the assets in the Unity Project are from:
- 3D Models: Kenney.nl
- 2D Assets (GUI, HUD...): Kenney.nl
- SFX: Kenney.nl
- Font: Nunito — Designed by Vernon Adams, Cyreal & Jacques Le Bailly
- - Licensed under the SIL Open Font License, Version 1.1

## Error code index API's:

### First Number:

> Service number, to know from where comes

- 0: Gateway
- 1: Version Service
- 2: Auth Service
- 3: Matchmaking Service
- 4: Network Balancer Service
- 5: Game Service

### Second Number:

> Global index, to know what is the exact error

- 0: General Error / Unknow
- 1: Invalid Payload / Body
- 2: Error in the DB
- 3: Rate Limit execed
- 4: Forbidden authentication
- 5: Invalid State: Server closed or unknow
- 6: Expired
- 7: Not Found
