#!/usr/bin/env bash
# Stop hook: si wiki/log.md no se modificó en los últimos 15 minutos,
# bloquea el cierre del turno una vez y pide actualizar la wiki.
input=$(cat)
# Evita bucles: si ya se bloqueó en este turno, deja terminar.
if echo "$input" | grep -q '"stop_hook_active"[[:space:]]*:[[:space:]]*true'; then
  exit 0
fi

log="${CLAUDE_PROJECT_DIR:-.}/wiki/log.md"
[ -f "$log" ] || exit 0

now=$(date +%s)
mtime=$(stat -c %Y "$log" 2>/dev/null || echo 0)
if [ $((now - mtime)) -lt 900 ]; then
  exit 0
fi

cat <<'EOF'
{"decision": "block", "reason": "Antes de terminar: actualiza la wiki según CLAUDE.md §7.4. Agrega una entrada al final de wiki/log.md (## [AAAA-MM-DD] tipo | título), actualiza las páginas afectadas y wiki/index.md si creaste o renombraste páginas. Si nada cambió en la wiki, registra igualmente la entrada indicándolo."}
EOF
