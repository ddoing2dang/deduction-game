"""Submit one completed authoring batch to the running Unity Editor.

Standard library only. Never starts Unity, enters Play Mode, runs tests or builds.
The Pipeline API is used only to bootstrap an absent Editor queue, not to edit art.
"""
import argparse
import json
import os
from pathlib import Path
import time
import urllib.request
import uuid

LATEST = "Assets/Case464/Scenes/Hotel/Hotel_RoseBlack_Final_Map_RoomSigns_WideAisle.unity"
IDS = ("MATTO", "MARIHI", "NAZ", "BOKI", "JJIRO", "MOONIRE")
DIRECTIONS = ("N", "NE", "E", "SE", "S", "SW", "W", "NW")


def read_json(path):
    try:
        return json.loads(path.read_text(encoding="utf-8-sig"))
    except FileNotFoundError:
        return None


def atomic_json(path, data):
    temporary = path.with_suffix(".pending")
    temporary.write_text(json.dumps(data, ensure_ascii=False), encoding="utf-8")
    os.replace(temporary, path)


def pipeline(project, route, body=None):
    descriptor = read_json(project / "Library/Pipeline/.unity-pipeline-port")
    if not descriptor or Path(descriptor["projectPath"]).resolve() != project:
        raise RuntimeError("No Pipeline descriptor for this exact running Unity project.")
    if descriptor.get("mode") != "editor":
        raise RuntimeError("Pipeline descriptor is not a live Editor.")
    port = int(descriptor["port"])
    if not 1 <= port <= 65535:
        raise RuntimeError("Invalid local Editor port.")
    # Local credentials stay in memory; never print or store them in the repository.
    request = urllib.request.Request(
        "http://127.0.0.1:%d%s" % (port, route),
        data=None if body is None else json.dumps(body).encode("utf-8"),
        headers={"Authorization": "Bearer " + descriptor["evalToken"],
                 "Content-Type": "application/json"})
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def bootstrap(project, request):
    status = pipeline(project, "/api/editor_status")
    if Path(status.get("projectPath", "")).resolve() != project:
        raise RuntimeError("Live Editor project handshake did not match.")
    if status.get("playMode") != "stopped":
        raise RuntimeError("Exit Play Mode; no command was issued.")
    if status.get("compiling") or status.get("domainReloadInProgress"):
        return False  # wait, do not send another refresh
    scene = json.dumps(request["scene"])
    code = (
        "var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();"
        "if (!scene.IsValid() || !scene.isLoaded || scene.isDirty || scene.path != " + scene + ") "
        'throw new System.InvalidOperationException("Expected clean, saved active scene; nothing discarded.");'
        'UnityEditor.SessionState.SetString("Case464.AssetRefresh.BootstrapId", "' + request["id"] + '");'
        "UnityEditor.Compilation.CompilationPipeline.compilationFinished += context => {"
        "UnityEditor.EditorUtility.RequestScriptReload(); };"
        "UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);"
        'return new { queued = true, operation = "bootstrap_asset_refresh" };'
    )
    reply = pipeline(project, "/api/exec",
                     {"command": "script eval", "parameters": {"code": code},
                      "clientSource": "agent", "clientName": "codex"})
    payload = reply.get("result", {})
    if (reply.get("success") is False or reply.get("error")
            or payload.get("success") is False or payload.get("error")):
        raise RuntimeError("Editor bootstrap rejected: " + json.dumps(reply, ensure_ascii=False))
    return True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project", type=Path,
                        default=Path(__file__).resolve().parents[2] / "game/UnityProject")
    parser.add_argument("--scene", default=LATEST)
    parser.add_argument("--scripts-changed", action="store_true")
    parser.add_argument("--no-hotel-checks", action="store_true")
    parser.add_argument("--timeout", type=int, default=300)
    parser.add_argument("--check-sprite", action="append", default=[])
    args = parser.parse_args()
    project = args.project.resolve()
    if not (project / "Assets").is_dir():
        raise RuntimeError("Unity project Assets folder not found.")
    queue = project / "Library/Case464AssetRefresh"
    queue.mkdir(parents=True, exist_ok=True)
    # All file writes must be complete before this entry point is invoked.
    request_id = uuid.uuid4().hex
    heartbeat = read_json(queue / "heartbeat.json")
    live = bool(heartbeat and time.time() - (queue / "heartbeat.json").stat().st_mtime < 10
                and Path(heartbeat.get("project", "")).resolve() == project)
    if live and heartbeat.get("phase") not in ("Idle", "Complete", "Failed"):
        raise RuntimeError("Another authoring batch is active; it was not overwritten.")
    pending = read_json(queue / "request.json")
    if pending and not (queue / ("result_" + pending.get("id", "") + ".json")).exists():
        raise RuntimeError("An uncompleted request already exists; inspect it instead of retrying.")
    sprites = args.check_sprite
    if not args.no_hotel_checks:
        sprites = list(sprites) + [
            "Assets/Case464/NPC/MapIdle/%s/MapIdle_%s.png" % (npc, direction)
            for npc in IDS for direction in DIRECTIONS]
    request = {
        "id": request_id, "session": heartbeat["session"] if live else "",
        "project": str(project), "scene": args.scene,
        "scriptsChanged": args.scripts_changed, "saveScene": True,
        "checkHotelNpcs": not args.no_hotel_checks, "timeoutSeconds": args.timeout,
        "assets": [{"path": path, "type": "Sprite"} for path in dict.fromkeys(sprites)]
                  + [{"path": args.scene, "type": "SceneAsset"}]}
    atomic_json(queue / "request.json", request)
    started = time.monotonic()
    bootstrap_sent = live
    while time.monotonic() - started < args.timeout:
        result = read_json(queue / ("result_" + request_id + ".json"))
        if result:
            print(json.dumps(result, ensure_ascii=False, indent=2))
            return 0 if result["phase"] == "Complete" else 1
        if not bootstrap_sent:
            bootstrap_sent = bootstrap(project, request)
        time.sleep(.5)
    raise TimeoutError("Authoring batch timed out. No retry was issued; inspect request " + request_id)


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(json.dumps({"phase": "Blocked", "error": str(error)}, ensure_ascii=False))
        raise SystemExit(1)
