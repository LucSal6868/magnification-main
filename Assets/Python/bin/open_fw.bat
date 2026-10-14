@echo on

title GenTL Viewer: Configuring Windows Firewall
REM                                                                       
REM Opens up windows firewall to allow GenTL GigE packets through
REM                                                                       
REM    o  0                                                               
REM    | /       Copyright (c) 2022                                       
REM   (CL)---o   Critical Link, LLC                                       
REM     \                                                                 
REM      O                                                                
REM ***********************************************************************

if ""=="%1" goto errExit
if ""=="%2" goto errExit
set rule="GenTL Viewer"
set mainapp=%~1
if "show"=="%2" goto end

REM Check for elevation and runas if not
@pushd %~dp0 & fltmc 2>nul | find "." && (powershell start '%~f0' ' %*' -verb runas 2>nul && exit /b) 

if "uninstall"=="%2" goto uninstall

:install
    @echo on
    netsh advfirewall firewall add rule name=%rule% dir=in action=allow program="%mainapp%" enable=yes ^
    description="Allow access for GigE protocol"
    @goto end
    
:uninstall
    @echo on
    netsh advfirewall firewall delete rule name=%rule% program="%mainapp%"
    @goto end
    
:end
    netsh advfirewall firewall show rule %rule%
    if /I Not "%CMDCMDLINE:"=%" == "%COMSPEC%" Pause
    @goto :EOF
    
:errExit
    echo Usage:
    echo     %0 ^<program^> ^<install^|uninstall^|show^>
    
exit /b 0