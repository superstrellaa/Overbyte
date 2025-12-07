# Overbyte Project

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
