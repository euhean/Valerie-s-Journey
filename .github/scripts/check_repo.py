"""Repository health checks for a Unity project. Runs without Unity.

Each check prints the files it rejects. The script exits with 1 if any check fails.
Run it locally from the repository root:  python .github/scripts/check_repo.py
"""
import json
import re
import subprocess
import sys
from collections import defaultdict
from pathlib import PurePosixPath

failures = 0


def report(title, problems, hint):
    """Prints one check's result and counts it as failed when it found problems."""
    global failures
    if not problems:
        print(f"ok    {title}")
        return
    failures += 1
    print(f"FAIL  {title}")
    for p in problems:
        print(f"        {p}")
    print(f"      → {hint}")


def tracked_files():
    out = subprocess.run(["git", "ls-files", "-z"], capture_output=True, check=True).stdout
    return [f for f in out.decode("utf-8").split("\0") if f]


files = tracked_files()
file_set = set(files)

# Every folder that holds a tracked file, e.g. "Assets/Scripts" for "Assets/Scripts/A.cs".
folders = set()
for f in files:
    for parent in PurePosixPath(f).parents:
        if str(parent) != ".":
            folders.add(str(parent))

assets = [f for f in files if f.startswith("Assets/")]
asset_paths = {f for f in assets if not f.endswith(".meta")} | {d for d in folders if d.startswith("Assets/")}

# 1. Unity identifies assets by the GUID in their .meta file. An asset committed without
#    its .meta gets a new GUID on every clone, so every reference to it breaks.
missing_meta = sorted(p for p in asset_paths if p + ".meta" not in file_set)
report("every asset has a .meta file", missing_meta,
       "open the project in Unity so it generates them, then commit the .meta files.")

# 2. A .meta whose asset isn't committed (usually because .gitignore excludes it) means a
#    clone is missing that asset, and anything referencing it shows up as "Missing".
orphan_meta = sorted(f for f in assets if f.endswith(".meta") and f[:-5] not in asset_paths)
report("every .meta file has its asset", orphan_meta,
       "commit the asset (check .gitignore) or delete the .meta if the asset is gone.")

# 3. Windows and macOS treat "Aphla.unity" and "aphla.unity" as the same file, so a
#    checkout there silently loses one of them.
by_lower = defaultdict(list)
for p in file_set | folders:
    by_lower[p.lower()].append(p)
collisions = sorted(" vs ".join(sorted(v)) for v in by_lower.values() if len(v) > 1)
report("no paths that differ only in letter case", collisions,
       "rename or delete one of each pair with `git mv` / `git rm`.")

# 4. Leftover merge-conflict markers compile in no language and corrupt YAML assets.
marker = re.compile(rb"^(<{7}|>{7}) ", re.MULTILINE)
text_suffixes = (".cs", ".unity", ".prefab", ".asset", ".meta", ".anim", ".controller",
                 ".mat", ".json", ".inputactions", ".md", ".yml", ".yaml", ".txt")
conflicts = []
for f in files:
    if f.endswith(text_suffixes):
        with open(f, "rb") as fh:
            if marker.search(fh.read()):
                conflicts.append(f)
report("no merge-conflict markers", sorted(conflicts),
       "resolve the conflict in each file and remove the <<<<<<< / >>>>>>> lines.")

# 5. Without the package manifest a clone doesn't know it needs Input System or
#    TextMesh Pro, and the scripts that use them don't compile.
manifest = "Packages/manifest.json"
problems = []
if manifest not in file_set:
    problems.append(f"{manifest} is not committed")
else:
    with open(manifest, encoding="utf-8") as fh:
        try:
            json.load(fh)
        except json.JSONDecodeError as e:
            problems.append(f"{manifest} is not valid JSON: {e}")
report("package manifest is committed and valid", problems,
       "commit Packages/manifest.json from a working copy of the project.")

# 6. Scenes listed in File → Build Profiles must exist, or the build ships without them.
build_settings = "ProjectSettings/EditorBuildSettings.asset"
with open(build_settings, encoding="utf-8") as fh:
    listed = re.findall(r"^\s+path: (\S.*)$", fh.read(), re.MULTILINE)
problems = [f"{s} is in the build list but not committed" for s in listed if s not in file_set]
if not listed:
    problems.append("no scenes in the build list")
report("build scene list points at committed scenes", problems,
       "fix the scene list in File → Build Profiles.")

print()
print("All checks passed." if failures == 0 else f"{failures} check(s) failed.")
sys.exit(1 if failures else 0)
