Commit all changes and push to main. Steps:

1. Run `git status` to see what changed
2. Run `git diff` (staged + unstaged) to understand the changes
3. Run `git log --oneline -5` to match existing commit style
4. Run `dotnet clean` then `dotnet build` to verify the solution compiles
5. Run `dotnet test` to verify all tests pass
6. Build the MSI installer: `powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1`
7. Stage all relevant files (exclude secrets, .env, credentials, artifacts/)
8. Write a concise commit message summarizing the changes
9. Commit with: Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>
10. Push to origin main
11. Show the result
