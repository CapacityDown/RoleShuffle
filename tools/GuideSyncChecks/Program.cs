using System.Reflection;
using System.Text;
using ExitGames.Client.Photon;
using Photon.Pun;
using REPOJP.StageRoles;

int checks = 0;
void Check(bool pass, string label) { checks++; if (!pass) throw new Exception(label); }
bool NativeRejects(object value)
{
    try { NativeProtocol.RoundTrip(value); return false; }
    catch (TargetInvocationException error) when (error.InnerException is NotSupportedException) { return true; }
}
Check(NativeRejects(new string('A', 35183)), "Reproduce logged 35183-byte failure with installed Protocol18");
foreach (string value in new[] { "", new string('A', 32767), new string('A', 32768), new string('A', 35183), new string('A', 120000), new string('界', 20000), string.Concat(Enumerable.Repeat("🙂", 12000)) })
{
    object wire = RoleGuideWire.Encode(value);
    Check((wire is string) == (Encoding.UTF8.GetByteCount(value) <= short.MaxValue), "Choose encoding by UTF8 bytes, not characters");
    Check(RoleGuideWire.Decode(NativeProtocol.RoundTrip(wire)) == value, "Installed Photon preserves the complete text");
}
Check(RoleGuideWire.Decode(new byte[] { 0xc3, 0x28 }) == null, "Reject invalid UTF8");
Check(RoleGuideWire.Decode(new byte[RoleGuideWire.MaximumPayloadBytes + 1]) == null, "Bound incoming data");
Check(RoleGuideWire.Decode(17) == null, "Ignore unsupported property type");
try { RoleGuideWire.Encode(new string('A', RoleGuideWire.MaximumPayloadBytes + 1)); Check(false, "Oversized guide rejected"); }
catch (ArgumentException) { Check(true, "Oversized guide rejected before transport"); }
var config = new StageRolesConfig();
var room = new Room(); PhotonNetwork.CurrentRoom = room;
string full = RoleGuideSync.CurrentSignature(config);
Check(full.Length > short.MaxValue, "All-role configured bilingual guide crosses the old limit");
Check(NativeRejects(full), "Old guide publication would fail with current role catalog");
// Repair an unsafe value cached by a previous failed publication.
room.CustomProperties["RoleShuffleGuideV2"] = full;
RoleGuideSync.Publish(config);
Check(room.Sends == 1 && room.LastSent["RoleShuffleGuideV2"] is byte[], "Repair oversized string using production publish path");
RoleGuideSync.Publish(config);
Check(room.Sends == 1, "Identical binary payload does not republish");
var statusProperties = new Hashtable(); RoleGuideSync.AddProperties(statusProperties, config, true);
Check(((Hashtable)NativeProtocol.RoundTrip(statusProperties))["RoleShuffleGuideV2"] is byte[], "Forced status refresh uses safe wire representation too");
Check(RoleGuideSync.ReadPayload("RoleShuffleGuideV2") == full, "Status checks hash the complete reconstructed guide");
PhotonNetwork.IsMasterClient = false;
foreach (var language in Enum.GetValues<RoleGuideLanguage>())
foreach (var role in RoleCatalog.AllRoles)
    Check(RoleGuideSync.Read(config, language)[role] == RoleGuideCatalog.Description(role, config, language), "Every role and language retains host values");
string signature = RoleGuideSync.CurrentSignature(config);
PhotonNetwork.IsMasterClient = true;
config.PorterCapacity.Value = 12; config.DisabledRoles.Add(StageRole.Porter); RoleGuideSync.Invalidate(); RoleGuideSync.Publish(config);
PhotonNetwork.IsMasterClient = false;
Check(RoleGuideSync.CurrentSignature(config) != signature && !RoleGuideSync.IsVisible(StageRole.Porter, config), "Changed host settings invalidate signature and visibility");
Check(RoleGuideSync.Read(config, RoleGuideLanguage.Japanese)[StageRole.Porter].Contains("12"), "Changed values arrive in Japanese");
room.CustomProperties["RoleShuffleGuideV2"] = RoleGuideSync.ReadPayload("RoleShuffleGuideV2")!;
Check(RoleGuideSync.Read(config, RoleGuideLanguage.English).Count == RoleCatalog.AllRoles.Count, "Still reads legacy string-valued guides");
room.CustomProperties["RoleShuffleGuideV2"] = new byte[] { 0xc3, 0x28 };
Check(RoleGuideSync.Read(config, RoleGuideLanguage.English)[StageRole.Porter].Contains("12"), "Invalid binary guide falls back to legacy English");
room.CustomProperties.Remove("RoleShuffleGuideV1");
Check(RoleGuideSync.Read(config, RoleGuideLanguage.Japanese)[StageRole.Porter] == RoleGuideCatalog.GenericDescription(StageRole.Porter, RoleGuideLanguage.Japanese), "Missing or invalid host data falls back safely");
PhotonNetwork.IsMasterClient = true; PhotonNetwork.CurrentRoom = new Room { FailSend = true };
RoleGuideSync.Publish(config); RoleGuideSync.Publish(config);
Check(StageRolesPlugin.ModLogger.Warnings.Count == 1, "Display publication failure returns without aborting caller and logs once");
PhotonNetwork.CurrentRoom.FailSend = false; RoleGuideSync.Publish(config);
Check(PhotonNetwork.CurrentRoom.CustomProperties["RoleShuffleGuideV2"] is byte[], "Publication recovers when the connection recovers");
Console.WriteLine($"PASS: {checks} guide publication, native Photon Protocol18 round-trip, compatibility and failure-isolation checks. Catalog payload: {full.Length} bytes.");
