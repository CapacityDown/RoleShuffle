# Negotiator verification

Run `dotnet run --project tools/NegotiatorChecks -c Release`.

Runs the production entitlement state, runtime and patches with lightweight
Unity/player adapters. Verifies one non-stacking next-shop grant; death, role
changes, avatar reconnection, expiry and run resets; pending, reserved and used
states; shop role/budget publication and short notices; interruption retries;
consumption only after a real price reduction; ordinary negotiations unchanged.

Reads the installed R.E.P.O. v0.5.0 ShopKeeper IL to validate all four transpilers
and preserve native validation, rejection bookkeeping and RPC instructions.
An optional first argument supplies a different Assembly-CSharp.dll path.

This does not run Unity physics, Photon transport, TTS or a live shop scene.
