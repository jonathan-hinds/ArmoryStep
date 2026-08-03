# Unity Dashboard setup

These steps require the project owner and cannot be completed safely by repository code.

1. In Unity Editor, open `Edit > Project Settings > Services` and link this project to the correct Unity Cloud organization/project. Confirm `ProjectSettings/UnityConnectSettings.asset` contains the intended project ID before sharing builds.
2. In the Unity Dashboard, create or confirm `development` and `production` environments. `ServicesConfiguration.asset` currently uses `production`; change that asset when testing another environment.
3. Open Authentication for the project and accept any service terms. Anonymous sign-in needs no external identity-provider credentials, but the project must be linked and Authentication available.
4. Open Multiplayer Services and enable the services used by Sessions (Lobby and Relay). Review Relay pricing/usage limits and select the same environment as the client.
5. If your organization uses access controls, ensure developers and CI service accounts can read the project and deploy Multiplayer configuration.

## Identity caveat

Anonymous Authentication caches a session token and restores the same player while browser storage remains available. It cannot recover an identity after site data is cleared or the token is lost. Before progression or purchases ship, link anonymous accounts to a recoverable provider and design account-conflict handling.

## Connection validation

1. Enter Play Mode through `Bootstrap` and wait for the panel to show `Ready`.
2. Select `HOST`; copy the join code.
3. Run a second client from a development Web build or Multiplayer Play Mode and enter the code, then select `JOIN`.
4. Verify both clients reach `Connected`, then test `LEAVE`, host closure, lost network, tab suspension, and browser refresh.

Relay is explicitly configured to secure WebSockets (`WSS`) for both host and join. Session state exposes disconnect and host-change events; duel synchronization and host-state restoration do not exist yet.
