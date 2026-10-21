Windows only: `dotnet run --project tests/better_kbm/launcher_bitmap/LauncherBitmapTests.csproj -c Release`.
The regression writes an isolated synthetic PNG, uses the actual native-art loader,
deletes the original file and verifies both decoded and painted pixels. No game
assets are needed or modified. This test cannot execute on the Linux build host.
