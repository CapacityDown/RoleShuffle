# Guide synchronization regression checks

Run `dotnet run --project tools/GuideSyncChecks/GuideSyncChecks.csproj -c Release`.
Override `GameManagedDir` when R.E.P.O. is installed elsewhere. The test loads
the installed `Photon3Unity3D.dll` and runs its actual Protocol18 serializer and
deserializer, including the reported 35,183-byte failure and UTF-8 boundaries.
No game assembly or Photon library is committed to the repository.

Production guide publishing, receiving, cache invalidation, forced refresh,
legacy fallback and failure handling run against a room stand-in. Role text is
the production catalog; configuration values use the localization test fixtures.
Every configured role and all 14 supported languages are checked for exact text
preservation. This is real serialization, not a live Photon room or game session.

`tools/UtilityRuntimeChecks` additionally compiles the production status publisher:
large guides use the same wire format, status hashes match reconstructed data,
and publication failures wait before retrying instead of failing every frame.
