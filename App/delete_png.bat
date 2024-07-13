@echo off
setlocal

:: Confirmation prompt
echo This will delete all .png files in the current directory and all subdirectories.
set /p confirm=Are you sure you want to continue? (Y/N): 
if /I not "%confirm%"=="Y" (
    echo Operation cancelled.
    exit /b
)

:: Delete all .png files in the current directory and subdirectories
for /r %%f in (*.png) do (
    echo Deleting %%f...
    del "%%f"
    if %errorlevel% neq 0 (
        echo Error deleting %%f
    ) else (
        echo Successfully deleted %%f
    )
)

echo All .png files have been deleted.
endlocal
pause
