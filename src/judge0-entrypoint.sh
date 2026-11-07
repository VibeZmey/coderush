#!/bin/bash
set -euo pipefail

# The cgv2 image prepares controllers without sudo in its server/worker scripts.
# Delegate them before dropping to the image's regular application user.
cg_root=/sys/fs/cgroup
if [[ -f "$cg_root/cgroup.controllers" ]]; then
    mkdir -p "$cg_root/init"
    while read -r pid; do
        [[ -n "$pid" ]] || continue
        if ! printf '%s\n' "$pid" > "$cg_root/init/cgroup.procs"; then
            [[ ! -d "/proc/$pid" ]] || exit 1
        fi
    done < "$cg_root/cgroup.procs"
    printf '%s\n' "$$" > "$cg_root/init/cgroup.procs"
    printf '%s\n' '+cpu +memory +io +pids' > "$cg_root/cgroup.subtree_control"
fi

exec setpriv --reuid=judge0 --regid=judge0 --init-groups /api/docker-entrypoint.sh "$@"
