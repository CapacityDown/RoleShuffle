"""Compile production hit methods with deterministic stand-ins, not copied formulas."""
import re
from pathlib import Path
root=Path(__file__).resolve().parents[1]
source=(root/'StageRoleController.cs').read_text(encoding='utf-8')
def method(name):
 start=re.search(r'    private (?:static )?(?:void|bool|int) '+name+r'\(', source).start()
 brace=source.index('{',start);depth=1;i=brace+1
 while depth:
  depth+=(source[i]=='{')-(source[i]=='}');i+=1
 return source[start:i].replace('private ', 'internal ',1)
methods='\n'.join(method(n) for n in ('ApplyBrawlerEnemyDamage','TryGetBrawlerMultiplier','ScaleWeaponDamage','ApplyAvengerDamage','ApplySniperDamage'))
config=(root/'StageRolesConfig.cs').read_text(encoding='utf-8')
properties=[]
for name in sorted(set(re.findall(r'(?:_config|config)\.(\w+)\.Value',methods+(root/'CombatRoleRuntime.cs').read_text()+(root/'KingUpgradeAura.cs').read_text()))):
 match=re.search(rf'{name} = Bind(Int|Float)\(config, "[^"]+", "[^"]+", ([^,]+),',config)
 assert match,name
 properties.append(f'    internal Entry<{"int" if match[1]=="Int" else "float"}> {name} = new({match[2]});')
text='using System;\nusing UnityEngine;\nnamespace REPOJP.StageRoles;\ninternal sealed partial class StageRoleController {\n'+methods+'\n}\ninternal sealed class StageRolesConfig {\n'+'\n'.join(properties)+'\n}\n'
out=root/'tmp/combat-checks';out.mkdir(exist_ok=True,parents=True);(out/'Extracted.cs').write_text(text,encoding='utf-8')
# Check event routing and lifecycle contracts that surround the extracted methods.
event=(root/'EventRoleRuntime.cs').read_text();observe=event[event.index('internal void ObserveHealthUpdate'):event.index('private void ApplyWerewolfDamage')]
assert observe.index('if (!hit.EnemyOrigin)') < observe.index('NotifyAvengerAllyHit') < observe.index('ApplyBodyguardTransfer')
patch=(root/'LifecyclePatches.cs').read_text();assert 'ConfirmEnemyDamage(__instance, __0, attacker, __state.HealthBefore)' in patch
reset=source[source.index('private void ResetAssignmentForRoleChange'):source.index('internal bool IsRoleQueryRequest')]
assert 'BrawlerCombo.Reset()' in reset and 'GhostHealingUsed =' not in reset and 'HunterConfirmedKills =' not in reset
death=source[source.index('internal void PlayerDied'):source.index('private void TickAvenger')]
assert 'BrawlerCombo.Reset()' in death and 'GhostHealingUsed =' not in death
print('PASS: combat source extraction and event/lifecycle routing checks.')
