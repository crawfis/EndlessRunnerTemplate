# -*- coding: utf-8 -*-
"""Assemble each timebox's Canvas pages into one Markdown file students can hand to an AI.

    python docs/canvas/pull_from_canvas.py           # all timeboxes
    python docs/canvas/pull_from_canvas.py 2         # just timebox 2

Canvas is the source of truth here: this reads the live course pages, converts them
back to Markdown, and writes one self-contained file per timebox into docs/ai/.

Environment:
    CANVAS_TOKEN      a Canvas access token (Account -> Settings -> New Access Token)
    CANVAS_BASE_URL   e.g. https://osu.instructure.com
    CANVAS_COURSE_ID  optional; defaults to the id below

On Windows the token is usually stored at User scope, which a process started earlier
will not have inherited. Hydrate it for one command with:

    $env:CANVAS_TOKEN = [Environment]::GetEnvironmentVariable("CANVAS_TOKEN","User")

Requires pandoc on PATH. Never prints or writes the token.
"""
import io
import json
import os
import re
import subprocess
import sys
import urllib.error
import urllib.request
from datetime import date

DEFAULT_COURSE_ID = "220042"        # CSE 5912 AU2026 (4873)

# key -> (page-title pattern, output file, document title)
TIMEBOXES = {
    "1": (r"^(?P<n>\d\d) - (?P<title>.+)$", "timebox-1.md",
          "Timebox 1 — Studio Setup & Greenlight"),
    "2": (r"^TB2: (?P<n>\d\d) - (?P<title>.+)$", "timebox-2.md",
          "Timebox 2 — Design Wide, Build Narrow"),
    "3": (r"^TB3\+: (?P<n>\d\d) - (?P<title>.+)$", "timebox-3-plus.md",
          "Timebox 3 and Beyond — The Production Rhythm"),
}

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "docs", "ai")


def _api(path):
    base = os.environ["CANVAS_BASE_URL"].rstrip("/")
    req = urllib.request.Request(base + path,
                                 headers={"Authorization": "Bearer " + os.environ["CANVAS_TOKEN"]})
    with urllib.request.urlopen(req) as response:
        return json.loads(response.read().decode("utf-8"))


def _to_markdown(html):
    """Canvas page HTML -> GitHub-flavoured Markdown."""
    result = subprocess.run(
        ["pandoc", "-f", "html", "-t", "gfm", "--wrap=auto", "--columns=95"],
        input=html, capture_output=True, text=True, encoding="utf-8", check=True)
    return result.stdout.strip()


def build(key, pages, course_id):
    pattern, filename, doc_title = TIMEBOXES[key]
    matched = []
    for page in pages:
        m = re.match(pattern, page["title"])
        if m:
            matched.append((m.group("n"), m.group("title").strip(), page["url"]))
    matched.sort()
    if not matched:
        print("timebox %s: no pages matched %r" % (key, pattern))
        return

    parts = [
        "# %s" % doc_title,
        "",
        "> CSE 5912 Capstone. The complete Timebox %s assignment, assembled from the course"
        % key,
        "> pages on %s. Hand this file to your AI assistant as context before asking it to"
        % date.today().isoformat(),
        "> help you plan, estimate, or draft anything the assignment asks for.",
    ]
    for _, title, url in matched:
        body = _api("/api/v1/courses/%s/pages/%s" % (course_id, url)).get("body") or ""
        parts += ["", "## %s" % title, "", _to_markdown(body)]

    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)
    text = "\n".join(parts).rstrip() + "\n"
    io.open(os.path.join(OUT_DIR, filename), "w", encoding="utf-8", newline="\r\n").write(text)
    print("docs/ai/%-20s %2d pages, %6d chars, %5d words"
          % (filename, len(matched), len(text), len(text.split())))


def main():
    for var in ("CANVAS_TOKEN", "CANVAS_BASE_URL"):
        if not os.environ.get(var):
            sys.exit("%s is not set in this process - see the note at the top of this file." % var)
    course_id = os.environ.get("CANVAS_COURSE_ID") or DEFAULT_COURSE_ID
    keys = sys.argv[1:] or sorted(TIMEBOXES)
    try:
        pages = _api("/api/v1/courses/%s/pages?per_page=100" % course_id)
    except urllib.error.HTTPError as err:
        sys.exit("Canvas returned %s for course %s" % (err.code, course_id))
    print("course %s: %d pages\n" % (course_id, len(pages)))
    for key in keys:
        if key not in TIMEBOXES:
            sys.exit("unknown timebox %r - expected one of %s" % (key, ", ".join(sorted(TIMEBOXES))))
        build(key, pages, course_id)


if __name__ == "__main__":
    main()
