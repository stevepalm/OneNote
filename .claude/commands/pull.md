Pull latest changes from origin main. Steps:

1. Run `git status` to check for uncommitted changes
2. If there are uncommitted changes, stash them first with `git stash`
3. Run `git pull origin main`
4. If changes were stashed, run `git stash pop`
5. Run `dotnet clean` then `dotnet build` to compile the updated code
6. Build the MSI installer: `powershell -ExecutionPolicy Bypass -File scripts/build-installer.ps1`
7. Show the result
