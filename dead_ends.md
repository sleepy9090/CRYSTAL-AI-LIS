# Dead Ends & Investigation Notes

Read before starting a session. Append when stuck after 10+ tool calls with no progress.

Lifecycle: **Active** -> **Resolved** (prefix with RESOLVED + date once understood) -> delete after 20+ sessions.

## Entry format

## <Subsystem or Function Name>
- **Tried**: what approach was attempted
- **Failed because**: root cause of failure
- **Better approach**: what to do instead
- **Session**: NNN

## RESOLVED 2026-11-15 (session 17): Boss-gate flags F05C/F05D/F05E/F066 setter
- **Resolution**: never set. Their triggers `$98 $9B-$A0` are not placed by any area (`area_dump.py params`). The real boss gates are F100-F10C (`NpcHidden` presence lists for params `$C0-$CC`, set by the reward pickup). Lesson: check whether a flag's *tester* is reachable before hunting for its setter.
- **Tried**: abs/abs,X writes to `$6480+` (`flag_code_sites.py`); every `NpcFlagApply` caller (NPC B, hooks 0/1/2); set-list byte pairs `40 xx`/`00 xx` in banks 14/15; `9D/99 ?? 64` indexed writes in all code banks; `HitSetFlag` callers (`26:8FC4`, `30:CCF7` = clear path).
- **Failed because**: none of these sets the four flags. F05F/F062/F067/F06C turned out to be item-pickup set-lists (`ItemGetHook` `14:826F`), but these four have no matching record.
- **Better approach**: look at the boss areas' object types and their death code (bank 15/27), `(zp),Y` writes with runtime pointers, and callers that reach `26:9026` with `$12`=`$FF`. Or wait for the emulator (Ph5.5) and set a write watch on `$648B/$648C`.
- **Session**: 16
