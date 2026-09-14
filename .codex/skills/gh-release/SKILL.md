---
name: gh-release
description: "Assign a SemVer release tag to the latest commit on the master branch, using the app project version by default."
---

# Release Tag

Use this skill only for creating and pushing a Git tag. The repository GitHub Actions workflow creates the GitHub release automatically when the tag is pushed. Do not create a GitHub release directly.

Before the mutation, confirm that the user has supplied or clearly approved:

- Repository, in `owner/name` form.
- An exact release version, or permission to increment the project version.

Read the current release version from `<Version>` in `BluetoothMonitor.App\BluetoothMonitor.App.csproj`. This is the single semantic-version source for the app and the About page. The About page must read the generated informational assembly version, so it shows the same full value, including a beta suffix. Use an explicitly supplied exact SemVer version when provided. Otherwise, increment the patch version: `MAJOR.MINOR.PATCH` becomes `MAJOR.MINOR.(PATCH+1)`. Use the resulting version as a `v<version>` tag, as required by `.github/workflows/portable.yml`.

Before creating the tag, make sure `<Version>` in `BluetoothMonitor.App\BluetoothMonitor.App.csproj` exactly matches the calculated release version. Derive `<AppxPackageVersion>` and `Identity Version` in `BluetoothMonitor.App\Package.appxmanifest` from that same value as a four-part numeric version: use the beta number as the last component for beta releases, and use `.0` for stable releases. For example, release `1.2.3-beta.4` requires `1.2.3.4`, and release `1.2.3` requires `1.2.3.0`. The `AppxPackageVersion` property and manifest `Identity Version` must be equal so Windows shows the package version in Settings > Apps > Installed apps. Also update `assemblyIdentity version` in `BluetoothMonitor.App\app.manifest` to `<major>.<minor>.<patch>.0`. If any file does not match, update all required files, commit the version changes on `master`, and use the resulting latest `master` commit as the tag target. The commit message for this release commit must be the release notes themselves: generate the notes from commits since the previous release tag, exclude `chore` and `docs` commits, and include only functionality-related changes. Use those release notes as the commit message instead of `Bump app version to <version>`. Do not create the tag until the project version, Appx package version, package manifest version, app manifest version, About version, and tag version match.

Assign the tag to the latest commit on the `master` branch. Create and push only this tag after any required version-bump commit. Do not create or move a tag on another commit.

If the repository or exact version is missing and the default version increment is not approved, ask for it. Treat assigning the tag as an external mutation. Get explicit confirmation immediately before assigning it when the user's request does not already clearly authorize the mutation.

Create the calculated Git tag locally with target `master`, then push only that tag with `git push origin <tag>`. Do not use a GitHub tag API, the GitHub CLI, create a GitHub release, or perform unrelated repository operations. Report the repository, source version, calculated version, tag, and `master` target after success. If creating or pushing the tag fails, report the error and do not retry unless the user asks or the failure is clearly transient.
