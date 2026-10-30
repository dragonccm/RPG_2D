#!/usr/bin/env python3
"""P0-5 (GAME_MASTER_PLAN.md): guard chống đổi GUID trong .meta.

Chạy trước commit: python3 tools/check_meta.py [base_ref]
- So sánh GUID của mọi .meta thay đổi so với base (mặc định HEAD).
- Exit 1 nếu có GUID bị đổi (mất tham chiếu scene/prefab).
"""
import re
import subprocess
import sys

PROJ = "/home/dragonccm/RPG_Game_Current"
GUID_RE = re.compile(rb"^guid: ([0-9a-f]{32})$", re.M)


def guid_of(ref: str, path: str):
    r = subprocess.run(["git", "-C", PROJ, "show", f"{ref}:{path}"],
                       capture_output=True)
    if r.returncode != 0:
        return None
    m = GUID_RE.search(r.stdout)
    return m.group(1).decode() if m else None


def main():
    base = sys.argv[1] if len(sys.argv) > 1 else "HEAD"
    r = subprocess.run(["git", "-C", PROJ, "diff", "--name-only", base,
                        "--", "*.meta"], capture_output=True, text=True)
    changed = [l.strip() for l in r.stdout.splitlines() if l.strip().endswith(".meta")]
    if not changed:
        print(f"OK: không có .meta nào thay đổi so với {base}")
        return 0
    bad = []
    for path in changed:
        old, new = guid_of(base, path), guid_of(":", path)
        if old and new and old != new:
            bad.append((path, old, new))
    if bad:
        print("LỖI: GUID .meta bị đổi — sẽ mất tham chiếu scene/prefab:")
        for path, old, new in bad:
            print(f"  {path}: {old} → {new}")
        print("Khôi phục GUID cũ hoặc revert file.")
        return 1
    print(f"OK: {len(changed)} .meta thay đổi nhưng GUID giữ nguyên")
    return 0


if __name__ == "__main__":
    sys.exit(main())
