"""Report whether Vuforia has published an SDK newer than the one this binding wraps.

There is no machine-readable feed to subscribe to. Every candidate was checked:

  PTCInc/vuforia-engine on GitHub publishes real releases and tags, and is the obvious
    answer -- but it stopped at 11.3.4 on 2025-06-18, which is older than the 11.4.4 this
    repository already wraps. Abandoned as a source, and watched below as a canary in case
    it comes back.
  registry.packages.developer.vuforia.com is a genuine npm registry, responds without
    authentication, and redirects to Azure Artifacts with full metadata -- containing ten
    versions from 8.6.7 to 9.6.3, the newest published in November 2020. dist-tags.latest
    still reads 9.6.3.
  Maven Central and CocoaPods carry only third-party artifacts. The downloads page is an
    Angular application whose SDK list arrives after signing in. developer.vuforia.com's
    sitemap.xml and robots.txt return the application shell rather than the file, and
    library/sitemap.xml is real but stamps all 183 of its URLs with the same build date.
    There is no RSS or Atom anywhere.

So this scrapes the release notes, which is the only thing that works. That page is
fragile by nature, which decides the shape of everything below: the assertions matter more
than the happy path. A redesign that made the pattern match nothing would otherwise report
"no news" forever, and nobody would notice for as long as Vuforia kept shipping.

Exit codes: 0 checked successfully (whether or not something is new), 1 the check itself
could not be trusted.
"""

import json
import os
import re
import sys
import urllib.error
import urllib.request
from pathlib import Path

NOTES_URL = (
    "https://developer.vuforia.com/library/vuforia-engine/release-notes/"
    "vuforia-engine-release-notes/"
)

# The page returns 403 to anything that does not look like a browser. Not a block on
# automation as such -- it serves the full document to this and nothing else changes.
USER_AGENT = (
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 "
    "(KHTML, like Gecko) Chrome/130.0 Safari/537.36"
)

# Each release is an `<h_>Vuforia Engine v11.4.4</h_>` heading, newest first. Some carry a
# " Patch 1" suffix, which this deliberately ignores: the three numbers are the version.
VERSION = re.compile(r"Vuforia Engine v(\d+\.\d+\.\d+)")

# The floor that turns a broken scrape into a failure instead of a shrug. The page listed
# more than forty releases when this was written, going back to 10.x, so twenty is not a
# tight fit -- it is low enough to survive Vuforia trimming their history and high enough
# that a partial page or a changed markup pattern cannot slip past.
MIN_VERSIONS = 20

# Abandoned since 2025-06-18 at 11.3.4. Watched anyway: if PTC resumes publishing here it
# becomes the trustworthy source and the scraping above should be deleted, and that is
# worth one API call a month to find out.
CANARY_REPO = "PTCInc/vuforia-engine"

ISSUE_BODY = """\
Vuforia has published **{newer}**. This binding is generated from **{current}**.

Release notes: https://developer.vuforia.com/library/vuforia-engine/release-notes/vuforia-engine-release-notes/

## What has to be done by hand

The SDK is only downloadable after signing in to developer.vuforia.com and accepting the
EULA, so no workflow can fetch it. That part is a licensing decision and it stays with a
person.

1. Download the {newer} SDK for **Android** and **iOS**. Those two are the whole supported
   set; the Windows and UWP payloads were removed on purpose.
2. Replace `VuforiaGen/Headers/VuforiaEngine/` with the new headers.
3. Replace the two native binaries with the ones out of the same archive, so they cannot
   disagree with the headers:
   - `Evergine.Bindings.Vuforia/runtimes/android-arm64/native/libVuforiaEngine.so`
   - `Evergine.Bindings.Vuforia/buildTransitive/ios/VuforiaEngine.framework/VuforiaEngine`

   and the `VuforiaEngine.jar` beside them if the archive ships a new one.
4. Commit all of it to `main` in one go.

## What happens then, without you

That push triggers CD on `VuforiaGen/Headers/**`. It reads the new version out of
`VU_VERSION_MAJOR/MINOR/PATCH`, records it in `binding.yml`, regenerates the bindings,
commits the result, runs the API gate and publishes the package.

If the regeneration fails, `ci-doctor` classifies it and `binding-updater` repairs the
generator. Neither should need you.

## Worth reading while you are here

The API gate will report what {newer} added and what it removed. A removal is a breaking
change for everyone consuming this package, and it is better read than discovered.
"""


def fail(message):
    print(f"::error::{message}")
    sys.exit(1)


def parse(version):
    return tuple(int(part) for part in version.split("."))


def fetch(url, headers=None):
    request = urllib.request.Request(url, headers=headers or {"User-Agent": USER_AGENT})
    with urllib.request.urlopen(request, timeout=60) as response:
        return response.read().decode("utf-8", "replace")


def recorded_version():
    """The version this repository wraps, read from the manifest.

    Not maintained by hand: the vendored adapter reads it out of VuforiaEngine.h and
    writes it here, so it follows the headers automatically.
    """
    text = Path("binding.yml").read_text(encoding="utf-8")
    match = re.search(r"(?m)^\s*current:\s*(\S+)\s*$", text)
    if not match:
        fail("binding.yml has no upstream.release.current to compare against")
    return match.group(1)


def canary():
    """Newest release tag on the GitHub tracker, or None if it is still dormant."""
    token = os.environ.get("GH_TOKEN", "")
    headers = {"User-Agent": USER_AGENT, "Accept": "application/vnd.github+json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    try:
        releases = json.loads(
            fetch(f"https://api.github.com/repos/{CANARY_REPO}/releases?per_page=1", headers)
        )
    except (urllib.error.URLError, json.JSONDecodeError) as error:
        # Not fatal. This is a secondary signal, and failing the run because a canary
        # was unreachable would train everyone to ignore the red.
        print(f"::warning::could not read {CANARY_REPO} releases: {error}")
        return None
    return releases[0]["tag_name"].lstrip("v") if releases else None


def main():
    current = recorded_version()
    print(f"Recorded: {current}")

    try:
        page = fetch(NOTES_URL)
    except urllib.error.HTTPError as error:
        fail(f"{NOTES_URL} returned HTTP {error.code}. The page moved or started refusing us.")
    except urllib.error.URLError as error:
        fail(f"could not reach {NOTES_URL}: {error.reason}")

    found = sorted(set(VERSION.findall(page)), key=parse, reverse=True)
    print(f"Scraped {len(found)} version(s); newest {found[0] if found else 'none'}")

    # Two assertions, and they are the reason this script is trustworthy at all. Both
    # answer the same question -- "did I actually read the release notes?" -- because the
    # dangerous failure is not a wrong answer, it is a confident "nothing new" produced by
    # a pattern that no longer matches anything.
    if len(found) < MIN_VERSIONS:
        fail(
            f"only {len(found)} version(s) matched {VERSION.pattern!r} in {len(page):,} "
            f"bytes, expected at least {MIN_VERSIONS}. Treat this as a broken scrape "
            "rather than as an absence of releases, and check whether the page changed "
            "shape before touching this floor."
        )
    if current not in found:
        fail(
            f"the recorded version {current} is not among the {len(found)} scraped from "
            "the release notes. Either the manifest records something Vuforia never "
            "published, or the pattern is now reading the wrong part of the page."
        )

    newest = found[0]
    tracker = canary()
    if tracker:
        print(f"Canary {CANARY_REPO}: {tracker}")
        if parse(tracker) > parse(current):
            print(
                f"::notice::{CANARY_REPO} has published {tracker}. It was dormant when "
                "this was written; if it is current again, prefer it and delete the scrape."
            )

    newer = newest if parse(newest) > parse(current) else ""
    if newer:
        # Written here rather than in the workflow because a heredoc inside a YAML block
        # scalar cannot start at column 0: every line would carry the block's indentation
        # into the issue body, and GitHub would render the whole thing as a code block.
        Path("issue-body.md").write_text(ISSUE_BODY.format(newer=newer, current=current),
                                        encoding="utf-8")

    with open(os.environ["GITHUB_OUTPUT"], "a", encoding="utf-8") as fh:
        fh.write(f"newer={newer}\n")
        fh.write(f"current={current}\n")
        fh.write(f"newest={newest}\n")

    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as fh:
            fh.write("### Vuforia Engine SDK\n\n")
            fh.write(f"- wrapped here: `{current}`\n")
            fh.write(f"- newest published: `{newest}`\n")
            fh.write(f"- versions scraped: {len(found)}\n")
            if tracker:
                fh.write(f"- {CANARY_REPO}: `{tracker}`\n")
            fh.write(
                f"\n{'**A newer SDK is available.**' if newer else 'Up to date.'}\n"
            )

    print(f"Newest published: {newest} -- {'newer' if newer else 'already wrapped'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
