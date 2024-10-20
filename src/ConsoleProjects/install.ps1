# Define the source and destination paths
$sourceFilePath = "app.exe"
$destinationFolderPath = "$env:LOCALAPPDATA\app"
$destinationFilePath = "$destinationFolderPath\app.exe"

# Create the destination folder if it doesn't exist
if (!(Test-Path -Path $destinationFolderPath)) {
    New-Item -ItemType Directory -Path $destinationFolderPath
}

# Copy the .exe file to the destination folder
Copy-Item -Path $sourceFilePath -Destination $destinationFilePath -Force

# Define the shortcut path
$shortcutPath = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\app.lnk"

# Create a WScript.Shell COM object
$WScriptShell = New-Object -ComObject WScript.Shell

# Create a shortcut
$shortcut = $WScriptShell.CreateShortcut($shortcutPath)

# Set the shortcut target path and other properties
$shortcut.TargetPath = $destinationFilePath
$shortcut.WorkingDirectory = $destinationFolderPath
$shortcut.WindowStyle = 1
$shortcut.Description = "App Shortcut"
$shortcut.Save()

Write-Output "Shortcut created successfully at $shortcutPath"
