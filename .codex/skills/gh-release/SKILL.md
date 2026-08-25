---
name: gh-release
description: "Assign a SemVer release tag to the latest commit on the master branch, using the app project version by default."
---

# Release Tag

Use this skill only for assigning a release tag. The repository workflow creates the GitHub release automatically when the tag is pushed. Do not create a release directly.

Before the mutation, confirm that the user has supplied or clearly approved:

- Repository, in `owner/name` form.
- An exact release version, or permission to increment the project version.

Read the current version from `<Version>` in `BluetoothMonitor.App\BluetoothMonitor.App.csproj`. Use an explicitly supplied exact SemVer version when provided. Otherwise, increment the patch version: `MAJOR.MINOR.PATCH` becomes `MAJOR.MINOR.(PATCH+1)`. Use the resulting version as a `v<version>` tag, as required by `.github/workflows/portable.yml`.

Assign the tag to the latest commit on the `master` branch. Create and push only this tag. Do not create or move a tag on another commit.

If the repository or exact version is missing and the default version increment is not approved, ask for it. Treat assigning the tag as an external mutation. Get explicit confirmation immediately before assigning it when the user's request does not already clearly authorize the mutation.

Use the available GitHub tag operation to create the calculated tag in `<owner/name>` with target `master`. Do not use the GitHub CLI, create a GitHub release, or perform unrelated repository operations. Report the repository, source version, calculated version, tag, and `master` target after success. If the tag operation fails, report the error and do not retry unless the user asks or the failure is clearly transient.
