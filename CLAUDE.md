@AGENTS.md

# Claude Code notes

- Skills in `.claude/skills` run as slash commands: `/add-event`, `/add-auto-chain`,
  `/add-bridge-mapping`, `/add-event-domain`, `/audit-events`, `/list-events`,
  `/generate-segments`, `/verify-unity`.
- Run `/verify-unity` after every C# edit (the rule itself is in AGENTS.md).
- Long Unity or build output goes to a file; read its end.
- `CLAUDE.local.md` (untracked) holds instructor-only notes; never copy it into a tracked file.
