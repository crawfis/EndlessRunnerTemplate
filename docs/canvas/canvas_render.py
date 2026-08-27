# -*- coding: utf-8 -*-
"""Shared rendering for the per-timebox Canvas publishers.

Not run directly. `build_timebox1.py` and `build_timebox2.py` each call `build()`
with their own source document and output directory.

Requires pandoc on PATH. Output is a Canvas-safe HTML fragment: no <html>/<head>
wrapper, no <script> or <style> (Canvas strips both), all styling inline.
"""
import io
import os
import re
import subprocess
import sys

TABLE = 'style="border-collapse:collapse;width:100%;margin:1em 0;"'
TH = ('style="border:1px solid #c7cdd1;padding:6px 10px;text-align:left;'
      'background-color:#f5f5f5;vertical-align:top;"')
TD = 'style="border:1px solid #c7cdd1;padding:6px 10px;vertical-align:top;"'
PRE = ('style="background-color:#f5f5f5;border:1px solid #c7cdd1;padding:8px 10px;'
       'overflow-x:auto;"')


def render(md_text):
    """Markdown (GFM) -> Canvas-safe HTML fragment with inline styling."""
    tmp = os.path.join(os.environ.get("TEMP", "."), "_canvas_chunk.md")
    io.open(tmp, "w", encoding="utf-8", newline="\n").write(md_text)
    html = subprocess.run(
        ["pandoc", tmp, "-f", "gfm", "-t", "html5", "--wrap=none"],
        capture_output=True, text=True, encoding="utf-8", check=True).stdout
    html = re.sub(r"<table>", "<table %s>" % TABLE, html)
    html = re.sub(r"<th\b[^>]*>", "<th %s>" % TH, html)
    html = re.sub(r"<td\b[^>]*>", "<td %s>" % TD, html)
    html = re.sub(r"<colgroup>.*?</colgroup>\n?", "", html, flags=re.S)
    html = re.sub(r"<pre\b[^>]*>", "<pre %s>" % PRE, html)
    html = re.sub(r'<code class="[^"]*">', "<code>", html)
    return html


def _banner(title, source_name, builder, extra=""):
    return ("<!-- %s\n"
            "     Generated from docs/%s by docs/canvas/%s --\n"
            "     edit the Markdown, never this file. Paste into a Canvas page via the Rich\n"
            "     Content Editor's HTML view (the </> button). Canvas strips <script>/<style>,\n"
            "     so all styling here is inline.%s -->\n"
            % (title, source_name, builder, extra))


def _slug(text):
    text = text.lower().replace("&", "and")
    text = re.sub(r"[^a-z0-9]+", "-", text).strip("-")
    return re.sub(r"-+", "-", text)[:48]


def build(label, source, out_dir):
    """Publish `source` as one combined page plus one page per '##' section.

    label    e.g. "Timebox 2" - prefixes every suggested Canvas page title
    source   path to the requirements Markdown
    out_dir  directory to write the split pages into
    """
    builder = os.path.basename(sys.argv[0]) or "build_timebox.py"
    source = os.path.abspath(source)
    out_dir = os.path.abspath(out_dir)
    source_name = os.path.basename(source)
    md = io.open(source, encoding="utf-8", newline="").read().replace("\r\n", "\n")

    combined = os.path.splitext(source)[0] + ".html"
    io.open(combined, "w", encoding="utf-8", newline="\r\n").write(
        _banner("%s requirements -- complete, single page" % label, source_name, builder)
        + render(md))

    parts = re.split(r"(?m)^## ", md)
    front = re.sub(r"(?m)^# .*\n", "", parts[0]).strip()   # Canvas shows the page title itself
    sections = []
    for chunk in parts[1:]:
        newline = chunk.index("\n")
        sections.append((chunk[:newline].strip(), chunk[newline + 1:].rstrip()))

    if not os.path.isdir(out_dir):
        os.makedirs(out_dir)

    rows = []
    for index, (title, body) in enumerate(sections, 1):
        page_title = "%s: %s" % (label, title)
        text = (front + "\n\n" + body) if index == 1 else body
        name = "%02d-%s.html" % (index, _slug(title))
        io.open(os.path.join(out_dir, name), "w", encoding="utf-8", newline="\r\n").write(
            _banner("Canvas page title: %s" % page_title, source_name, builder,
                    "\n     Module item %d of %d." % (index, len(sections)))
            + render(text))
        rows.append((index, title, name))

    idx = ["The %s assignment, one page per module item. Read them in order the first time; "
           "after that, use this list to jump.\n" % label, "| # | Page |", "|---|---|"]
    idx += ["| %d | %s |" % (i, t) for i, t, _ in rows]
    io.open(os.path.join(out_dir, "00-index.html"), "w", encoding="utf-8", newline="\r\n").write(
        _banner("Canvas page title: %s: Start here" % label, source_name, builder)
        + render("\n".join(idx)))

    root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    print("combined: %s" % os.path.relpath(combined, root).replace("\\", "/"))
    print("split:    %s/  (%d pages + index)"
          % (os.path.relpath(out_dir, root).replace("\\", "/"), len(sections)))
    for i, t, n in rows:
        print("  %s  %s: %s" % (n.ljust(46), label, t))
