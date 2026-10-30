#!/usr/bin/env python3
"""Download verified CC0 VRoid hair sample (whole VRM avatar) with integrity check."""
from __future__ import annotations

import hashlib
from pathlib import Path
from urllib.request import Request, urlopen

URL = (
    "https://raw.githubusercontent.com/madjin/vrm-samples/"
    "e16eb187100149a315ad92c3c9968f1d5baa6c7d/"
    "vroid/beta/HairSample_Female.vrm"
)
EXPECTED_SIZE = 17518216
EXPECTED_GIT_BLOB_SHA1 = "0e5235f9a7972fbcbceb74960479b208c27a8707"
DEST = Path(__file__).resolve().parent / "HairSample_Female.vrm"


def git_blob_hash(data: bytes) -> str:
    header = f"blob {len(data)}\0".encode("ascii")
    return hashlib.sha1(header + data).hexdigest()


def main() -> None:
    if DEST.exists():
        old = DEST.read_bytes()
        if len(old) == EXPECTED_SIZE and git_blob_hash(old) == EXPECTED_GIT_BLOB_SHA1:
            print(f"Verified existing CC0 asset: {DEST}")
            return

    req = Request(URL, headers={"User-Agent": "deduction-game-cc0-asset-fetch/1.0"})
    with urlopen(req, timeout=120) as res:
        data = res.read(EXPECTED_SIZE + 1)

    actual = git_blob_hash(data)
    if len(data) != EXPECTED_SIZE or actual != EXPECTED_GIT_BLOB_SHA1:
        raise ValueError(
            f"Asset integrity mismatch: bytes={len(data)}, blob_sha={actual}"
        )

    temp = DEST.with_suffix(".vrm.tmp")
    temp.write_bytes(data)
    temp.replace(DEST)
    print(f"Downloaded and verified CC0 asset: {DEST}")


if __name__ == "__main__":
    main()
