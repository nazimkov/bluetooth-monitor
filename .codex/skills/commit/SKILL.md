---
name: commit
description: Create commits for this project using the required commit format and verification commands.
---

# Commits

Use this skill when the user asks to commit changes in this repository.

## Commit format

Use:

`<type>(<scope>): <subject>`

Keep the subject short and use an imperative verb. Choose a type and scope that describe the actual change.

## Required checks

Before creating the commit, inspect the staged file list. Build the whole solution and run all tests only when the staged changes include source or project files, such as `.cs`, `.xaml`, `.resx`, `.props`, `.targets`, or `.csproj` files. Skip these checks for changes that contain none of these file types.

When checks are required, run:

```powershell
dotnet build BluetoothMonitor.slnx -c Debug -p:Platform=x64
dotnet test BluetoothMonitor.E2E -c Debug -p:Platform=x64 --logger "console;verbosity=detailed"
```

If a check fails, do not create the commit. Report the failure and its relevant output.

Review the staged diff and confirm that only the requested changes are staged. Do not stage unrelated user changes.
