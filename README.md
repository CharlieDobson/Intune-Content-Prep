# Intune Content Prep

A small WPF (.NET 10) front end for **IntuneWinAppUtil.exe** that turns a
**PSAppDeployToolkit v4** package folder into an `.intunewin` file for an Intune Win32 app.

- **Source folder**: the PSADT v4 package root. It must contain `Invoke-AppDeployToolkit.exe`,
  which is always used as the setup file.
- **Output folder**: where `Invoke-AppDeployToolkit.intunewin` is written. It's created if it
  doesn't exist. It can't be the source folder or a folder inside it.
- **IntuneWinAppUtil.exe**: the tool to run (see below). A folder that contains it also works.
- **Create** runs

  ```
  IntuneWinAppUtil.exe -c <source> -s Invoke-AppDeployToolkit.exe -o <output> -q
  ```

  and streams its output into the terminal panel that opens below the inputs.

## Behaviour notes

- **`-q` is always used**, so IntuneWinAppUtil never prompts. `-q` also makes it overwrite an
  existing `Invoke-AppDeployToolkit.intunewin` without asking, so the app asks first. Other files in
  the output folder are never touched.
- **IntuneWinAppUtil exits with code 0 even when it fails** (it prints `ERROR ...` and then
  `Done!!!`). The app only reports success when there's no `ERROR` line *and* the `.intunewin` was
  actually written during the run. Click the path in the result line to show the file in Explorer.
- The hint under the source box shows the app name/version from `Invoke-AppDeployToolkit.ps1`
  (when filled in) and the PSADT version from `PSAppDeployToolkit\PSAppDeployToolkit.psd1`, so you
  can confirm you picked the right package.
- Folders (or the tool exe) can also be dropped onto the boxes from Explorer, and pasted
  "Copy as path" values (with quotes) work too.
- Exiting during a run asks first, then stops IntuneWinAppUtil and deletes the partial
  `.intunewin`.

## Where IntuneWinAppUtil.exe comes from

The **IntuneWinAppUtil.exe** box is what gets run. Set it with Browse… (or by typing or dropping a
path), and it's remembered between launches. A location you've set always wins.

Only when the box is empty does the app fill it automatically, looking:

1. Beside `IntuneContentPrep.exe` (the intended deployed layout: put both exes in one folder)
2. On `PATH`

The hint under the box says which applies: *Found beside this app*, *Found on PATH* or
*Remembered between launches*. Clear the box to go back to automatic detection next time.

Settings (tool location, last folders) live in `%LOCALAPPDATA%\IntuneContentPrep\settings.json`.
Delete that file to reset them.

## Build

```powershell
dotnet build                                    # Debug build, bin\Debug\net10.0-windows\
dotnet publish -p:PublishProfile=Release        # single self-contained exe -> publish\IntuneContentPrep.exe
```

The published exe is self-contained (no .NET runtime needed on the target machine) and
compressed, and it's the only file in `publish\`.
