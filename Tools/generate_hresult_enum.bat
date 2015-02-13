set TT11 = C:\Program Files (x86)\Common Files\Microsoft Shared\TextTemplating\11.0\
set TT12 = C:\Program Files (x86)\Common Files\Microsoft Shared\TextTemplating\12.0\

cd %1\Tools\

IF EXIST %TT12% GOTO 12
IF EXIST %TT11% GOTO 11

:12
%TT12%\TextTransform.exe -out %2Utilities\HresultEnum.cs" "%2T4 templates\GenerateFromCSV.tt"
GOTO EOF

:11
%TT11%\TextTransform.exe -out %2Utilities\HresultEnum.cs" "%2T4 templates\GenerateFromCSV.tt"
GOTO EOF

:EOF