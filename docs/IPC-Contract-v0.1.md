# Cooldown IPC Contract v0.1

Single source of truth for Phase 2 named pipe IPC between Cooldown.Desktop (client) and Cooldown.Service (server). Scope: JSON over named pipes, request/response plus events, no persistence or process killing described here.

## 1. Introduction
- Purpose: define the transport, envelope, commands, events, errors, and versioning for Desktop ↔ Service communication.
- Audience: developers implementing P2.2-02..P2.2-05 (IPC plumbing and handlers).
- Out of scope: networking, cloud auth, persistence, process termination rules (those land in later phases).

## 2. Transport & Connection Model
- Transport: Windows Named Pipes.
- Pipe name: `\\.\pipe\Cooldown.Service.IPC` (may become configurable later).
- Roles: Desktop = client; Service = server (listener/acceptor).
- Pattern: duplex request/response on a single connection. One active desktop client per user session for Phase 2.
- Framing: UTF-8 JSON messages delimited by newline (`\n`). Length-prefixing can be added later if needed.
- Encoding: UTF-8.
- Lifetime: desktop connects at startup, reuses the pipe, reconnects with backoff if the service restarts.

## 3. Message Envelope (Base Schema)
All messages (both directions) share this envelope:

```json
{
  "protocolVersion": 1,
  "messageType": "Command",
  "command": "Lock.Create",
  "correlationId": "c2b40a10-5b5e-4b64-af39-8d6c3cde1b5f",
  "timestampUtc": "2025-12-06T12:34:56.789Z",
  "payload": { /* command- or event-specific */ }
}
```

Fields:
- `protocolVersion` (int): protocol contract version (v1 for Phase 2).
- `messageType` (string): `"Command"`, `"Response"`, or `"Event"`.
- `command` (string): command/event identifier (e.g., `Lock.Create`, `Service.Ping`).
- `correlationId` (string GUID): echoed in responses to pair with requests. For events, optional/omitted.
- `timestampUtc` (ISO 8601): sender timestamp.
- `payload` (object): command/event-specific body.

Direction conventions:
- Commands: Desktop → Service (`messageType = "Command"`).
- Responses: Service → Desktop (`messageType = "Response"`, same `correlationId` as request).
- Events: Service → Desktop (`messageType = "Event"`, no request required).

## 4. Command Set (Requests & Responses)

### 4.1 Lock.Create
- Command: `Lock.Create`
- Request payload:
```json
{
  "type": "Soft",
  "durationSeconds": 1800,
  "blockedApps": [ "Steam", "RiotClientServices", "LeagueOfLegends" ]
}
```
- Response payload (success):
```json
{
  "lockId": "c2b40a10-5b5e-4b64-af39-8d6c3cde1b5f",
  "type": "Soft",
  "startedAtUtc": "2025-12-06T12:00:00Z",
  "expiresAtUtc": "2025-12-06T12:30:00Z",
  "durationSeconds": 1800,
  "blockedApps": [ "Steam", "RiotClientServices", "LeagueOfLegends" ]
}
```

### 4.2 Lock.Cancel
- Command: `Lock.Cancel`
- Request payload:
```json
{}
```
- Response payload (success when something was canceled):
```json
{
  "canceled": true,
  "previousLockId": "c2b40a10-5b5e-4b64-af39-8d6c3cde1b5f"
}
```
- Response payload (no active lock):
```json
{
  "canceled": false,
  "reason": "NoActiveLock"
}
```

### 4.3 Lock.GetState
- Command: `Lock.GetState`
- Request payload:
```json
{}
```
- Response payload (active lock):
```json
{
  "hasActiveLock": true,
  "lock": {
    "lockId": "c2b40a10-5b5e-4b64-af39-8d6c3cde1b5f",
    "type": "Hard",
    "startedAtUtc": "2025-12-06T12:00:00Z",
    "expiresAtUtc": "2025-12-06T12:30:00Z",
    "durationSeconds": 1800,
    "remainingSeconds": 1200,
    "blockedApps": [ "Steam", "RiotClientServices" ]
  }
}
```
- Response payload (no lock):
```json
{
  "hasActiveLock": false
}
```

### 4.4 Apps.UpdateBlocked
- Command: `Apps.UpdateBlocked`
- Request payload:
```json
{
  "blockedApps": [ "Steam", "BattleNet", "RiotClientServices" ]
}
```
- Response payload:
```json
{
  "updated": true,
  "blockedApps": [ "Steam", "BattleNet", "RiotClientServices" ]
}
```

### 4.5 Apps.GetBlocked
- Command: `Apps.GetBlocked`
- Request payload:
```json
{}
```
- Response payload:
```json
{
  "blockedApps": [ "Steam", "RiotClientServices", "LeagueOfLegends" ]
}
```

### 4.6 Service.Ping
- Command: `Service.Ping`
- Request payload:
```json
{
  "clientVersion": "0.2.1-desktop"
}
```
- Response payload:
```json
{
  "serviceVersion": "0.2.1-service",
  "uptimeSeconds": 1234,
  "protocolVersion": 1
}
```

## 5. Events / Notifications (Service → Desktop)

### 5.1 Lock.StatusChanged
- Event: `Lock.StatusChanged`
- Payload:
```json
{
  "hasActiveLock": true,
  "lock": {
    "lockId": "c2b40a10-5b5e-4b64-af39-8d6c3cde1b5f",
    "type": "Soft",
    "startedAtUtc": "2025-12-06T12:00:00Z",
    "expiresAtUtc": "2025-12-06T12:30:00Z",
    "durationSeconds": 1800,
    "remainingSeconds": 900,
    "blockedApps": [ "Steam" ]
  },
  "reason": "Created" // or "Canceled", "Expired"
}
```
- Future event ideas (not required in Phase 2.2): `Apps.BlockEvent`, `Service.Warning`, `Service.Error`.

## 6. Error Handling & Error Codes
- Responses keep the envelope; `payload` contains either the success body or an error object.
- Error payload shape:
```json
{
  "success": false,
  "error": {
    "code": "NoActiveLock",
    "message": "Human-readable message.",
    "details": { }
  }
}
```
- Example codes:
  - `NoActiveLock`
  - `LockAlreadyActive`
  - `InvalidArguments`
  - `InternalError`
  - `Unauthorized`
- Rules:
  - `success: true` → payload is the command response body.
  - `success: false` → payload is the error object.
  - Unsupported commands in this protocol version should return `InvalidArguments` or a version-specific code, not crash.

## 7. Versioning Strategy
- `protocolVersion = 1` for Phase 2.
- Backward-compatible evolution only: do not remove or rename existing fields in v1; add optional fields instead.
- If a client sends a command that the service does not support, respond with an error code (do not close the pipe).
- `Service.Ping` can be used for capability checks; a future `Service.Negotiate` can be added if needed.

## 8. Security & Trust Model (Phase 2 scope)
- Same-machine only; Desktop and Service run under the current user context.
- Pipe ACLs should restrict access to the current user/session.
- No extra auth token in Phase 2.
- No PII; only lock/config state and app identifiers.
- Future phases may add stronger auth or multi-device considerations.

## 9. Concurrency & Lifetime Rules
- Single active desktop client per user session in Phase 2.
- Desktop opens the pipe once, reuses it, and reconnects with backoff if the service restarts.
- Commands are request/response; processing may be async internally, but behavior is logically synchronous per message.
- No long-lived streaming in Phase 2.

## 10. Future Extensions (Phase 3+ ideas)
- `Lock.Extend` — extend current lock duration.
- `EmergencyUnlock.Request` — friction-based unlock flow.
- `Service.TamperEvent` — notify of tamper detection.
- `Stats.GetSummary` — daily/weekly stats queries.
- Binary payload optimization or length-prefixed framing for higher throughput if needed later.
