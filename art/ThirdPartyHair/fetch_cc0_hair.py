#!/usr/bin/env python3
"""Fetch CC0 VRoid sample avatars containing hair, verifying exact pinned Git blobs."""
from __future__ import annotations

import hashlib
from pathlib import Path
from urllib.request import Request, urlopen

SRC = (
    "https://raw.githubusercontent.com/madjin/vrm-samples/"
    "e16eb187100149a315ad92c3c9968f1d5baa6c7d/vroid/beta/"
)
ASSETS = {
    "HairSample_Female.vrm": (17518216, "0e5235f9a7972fbcbceb74960479b208c27a8707"),
    "HairSample_Male.vrm": (18481892, "46e8c583ce5df3b8ee167141b59ba3ad583a2687"),
    "Sendagaya_Shino.vrm": (15074328, "91f7c9e9ec990efb9de305e4c1d0e32a46498830"),
    "Victoria_Rubin.vrm": (15479480, "3c59603eee2028756ed41ed4930dca6d99330f1e"),
}


def git_blob_sha(data: bytes) -> str:
    return hashlib.sha1(f"blob {len(data)}\0".encode() + data).hexdigest()


def main() -> None:
    destdir = Path(__file__).resolve().parent
    for name, (size, sha) in ASSETS.items():
        dest = destdir / name
        if dest.is_file():
            data = dest.read_bytes()
            if len(data) == size and git_blob_sha(data) == sha:
                print(f"Verified existing CC0: {name}")
                continue
        request = Request(SRC + name, headers={"User-Agent": "deduction-game-cc0-import/1.0"})
        with urlopen(request, timeout=120) as response:
            data = response.read(size + 1)
        real_sha = git_blob_sha(data)
        if len(data) != size or real_sha != sha:
            raise ValueError(f"Integrity failure: {name}; got {len(data)} bytes, {real_sha}")
        temp = dest.with_name(dest.name + ".part")
        temp.write_bytes(data)
        temp.replace(dest)
        print(f"Downloaded and verified CC0: {name}")


if __name__ == "__main__":
    main()
