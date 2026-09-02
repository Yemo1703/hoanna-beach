[Setup]
AppName=Hoanna Beach Control
AppVersion=1.1.0
AppPublisher=Hoanna Beach
AppPublisherURL=https://github.com/Yemo1703/hoanna-beach
DefaultDirName={commonpf}\Hoanna Beach Control
DefaultGroupName=Hoanna Beach Control
OutputDir=.\Output
OutputBaseFilename=HoannaBeachSetup
SetupIconFile=src\XR18BarControl\Assets\beer.ico
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
UsePreviousAppDir=no
CloseApplications=yes
RestartApplications=yes

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "src\XR18BarControl\bin\Release\net8.0-windows\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "src\XR18BarControl\Assets\beer.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\Hoanna Beach Control"; Filename: "{app}\XR18BarControl.exe"; IconFilename: "{app}\beer.ico"; WorkingDir: "{app}"
Name: "{group}\{cm:UninstallProgram,Hoanna Beach Control}"; Filename: "{uninstallexe}"
Name: "{commondesktop}\Hoanna Beach Control"; Filename: "{app}\XR18BarControl.exe"; IconFilename: "{app}\beer.ico"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\XR18BarControl.exe"; Description: "{cm:LaunchProgram,Hoanna Beach Control}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
