# blank code desktop preview

this is the first native windows app foundation for blank code. it uses wpf on .net 8 and does not use electron or a webview.

## run it

requirements: windows 10/11 and the .net 8 sdk.

from the repository root, run:

    dotnet run --project .\app\BlankCode.Desktop\BlankCode.Desktop.csproj

## included

- welcome/onboarding screen with new project, open folder, and sample project actions
- optional sign-in screen for github, google, and email
- local project creation and folder opening
- basic file explorer, text editing, and saving
- workspace shell with terminal/output and ai assistant panels
- local-first behavior: no forced login, no fake authentication, no credentials collected by the preview

## honest limitations

this is a ui and local-workspace prototype, not a complete ide yet. hosted authentication is not connected, and the sign-in buttons explain that rather than pretending a login succeeded. the terminal/build runner, git actions, syntax highlighting, model provider, and agent execution are not connected yet.

before shipping hosted login, select and configure the auth provider, register native oauth redirects, use authorization code + pkce and state validation, and store tokens with windows credential protection. do not ship client secrets inside the app.
