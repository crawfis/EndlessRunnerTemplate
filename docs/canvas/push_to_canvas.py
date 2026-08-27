# -*- coding: utf-8 -*-
"""Publish the generated Canvas pages straight into the course, replacing their bodies.

    python docs/canvas/push_to_canvas.py --dry-run     # report what would change
    python docs/canvas/push_to_canvas.py               # push every timebox
    python docs/canvas/push_to_canvas.py 2             # push just timebox 2

Pages are matched by their leading two-digit number, not by title or slug, so renaming
a page in Canvas does not break the mapping. Only the body is written: titles, module
placement, and anything else you set in Canvas are left alone. Index pages are never
touched.

Every page body is saved to a timestamped backup directory before it is overwritten,
and pages whose text already matches are skipped.

Environment: CANVAS_TOKEN, CANVAS_BASE_URL, optional CANVAS_COURSE_ID. On Windows the
token usually lives at User scope, which an older process has not inherited:

    $env:CANVAS_TOKEN = [Environment]::GetEnvironmentVariable("CANVAS_TOKEN","User")

Run docs/canvas/build_timebox*.py first so the local HTML is current.
"""
import io
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
from datetime import datetime

DEFAULT_COURSE_ID = "220042"        # CSE 5912 AU2026 (4873)

# key -> (Canvas page-title pattern, local directory)
TIMEBOXES = {
    "1": (r"^(?P<n>\d\d) - ", "timebox1"),
    "2": (r"^TB2: (?P<n>\d\d) - ", "timebox2"),
    "3": (r"^TB3\+: (?P<n>\d\d) - ", "timebox3plus"),
}

HERE = os.path.dirname(os.path.abspath(__file__))
BACKUPS = os.path.join(HERE, os.pardir, os.pardir, ".canvas-backups")


def _request(path, method="GET", payload=None):
    base = os.environ["CANVAS_BASE_URL"].rstrip("/")
    data = json.dumps(payload).encode("utf-8") if payload is not None else None
    req = urllib.request.Request(base + path, data=data, method=method, headers={
        "Authorization": "Bearer " + os.environ["CANVAS_TOKEN"],
        "Content-Type": "application/json",
    })
    with urllib.request.urlopen(req) as response:
        return json.loads(response.read().decode("utf-8"))


def _plain(html):
    """Rendered text, for deciding whether a page really changed."""
    result = subprocess.run(["pandoc", "-f", "html", "-t", "plain", "--wrap=none"],
                            input=html, capture_output=True, text=True, encoding="utf-8")
    return "\n".join(l.strip() for l in result.stdout.split("\n") if l.strip())


def _local_body(path):
    """The generated file minus its provenance comment."""
    text = io.open(path, encoding="utf-8").read()
    return (text[text.index("-->") + 3:] if "-->" in text else text).strip()


def push(key, pages, course_id, dry_run, backup_dir):
    pattern, folder = TIMEBOXES[key]
    remote = sorted((re.match(pattern, p["title"]).group("n"), p)
                    for p in pages if re.match(pattern, p["title"]))
    local_dir = os.path.join(HERE, folder)
    local = sorted(f for f in os.listdir(local_dir) if re.match(r"^(?!00)\d\d-.*\.html$", f))

    if len(remote) != len(local):
        print("timebox %s: %d pages in Canvas but %d generated locally - skipping"
              % (key, len(remote), len(local)))
        return 0

    pushed = 0
    for (number, page), filename in zip(remote, local):
        body = _local_body(os.path.join(local_dir, filename))
        current = _request("/api/v1/courses/%s/pages/%s" % (course_id, page["url"])).get("body") or ""
        if _plain(current) == _plain(body):
            print("  %s  %-46s unchanged" % (number, page["title"][:46]))
            continue
        io.open(os.path.join(backup_dir, "%s-%s.html" % (folder, page["url"])),
                "w", encoding="utf-8").write(current)
        if dry_run:
            print("  %s  %-46s WOULD UPDATE" % (number, page["title"][:46]))
        else:
            _request("/api/v1/courses/%s/pages/%s" % (course_id, page["url"]),
                     method="PUT", payload={"wiki_page": {"body": body}})
            print("  %s  %-46s updated" % (number, page["title"][:46]))
        pushed += 1
    return pushed


def main():
    args = [a for a in sys.argv[1:] if a != "--dry-run"]
    dry_run = "--dry-run" in sys.argv
    for var in ("CANVAS_TOKEN", "CANVAS_BASE_URL"):
        if not os.environ.get(var):
            sys.exit("%s is not set in this process - see the note at the top of this file." % var)
    course_id = os.environ.get("CANVAS_COURSE_ID") or DEFAULT_COURSE_ID

    backup_dir = os.path.abspath(os.path.join(
        BACKUPS, datetime.now().strftime("%Y%m%d-%H%M%S")))
    os.makedirs(backup_dir)

    try:
        pages = _request("/api/v1/courses/%s/pages?per_page=100" % course_id)
    except urllib.error.HTTPError as err:
        sys.exit("Canvas returned %s for course %s" % (err.code, course_id))

    total = 0
    for key in (args or sorted(TIMEBOXES)):
        if key not in TIMEBOXES:
            sys.exit("unknown timebox %r" % key)
        print("timebox %s:" % key)
        total += push(key, pages, course_id, dry_run, backup_dir)
    print("\n%s %d page(s). Backups of the previous bodies: %s"
          % ("would update" if dry_run else "updated", total,
             os.path.relpath(backup_dir, os.path.join(HERE, os.pardir, os.pardir))))


if __name__ == "__main__":
    main()
