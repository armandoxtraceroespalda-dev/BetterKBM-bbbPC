# Decorated settings shell regression

Windows and the .NET SDK are required:

    dotnet run --project tests/better_kbm/launcher_shell/LauncherShellTests.csproj -c Release

The STA/WPF test layers a capture dialog over the real LauncherPage shell and
checks logical ownership and layer order. It does not initialise Workspace,
read user settings, launch the game or write the user's configuration.
The old ControlsPage approach (adding Scroller directly to a new Grid) throws
before these assertions because Scroller is already owned by the shell Grid.
The test was added here but cannot run on the Linux packaging host.

UI23 also checks that sections can be prepared before attachment, repeated
preparation does not reparent rows, and a binding cell has no visual template
children while retaining native Button automation. This is a Windows STA test;
cross-compiling it on Linux does not execute these runtime assertions.
