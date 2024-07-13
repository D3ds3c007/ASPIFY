@echo off
setlocal

:: Path to cwebp.exe (ensure this path is correct or cwebp is in your PATH)
set CWEBP_PATH=cwebp

:: Quality of the output WebP images (0-100)
set QUALITY=80

:: Loop through all PNG files in the current directory
for %%f in (*.png) do (
    echo Converting %%f to %%~nf.webp...
    %CWEBP_PATH% -q %QUALITY% "%%f" -o "%%~nf.webp"
    if %errorlevel% neq 0 (
        echo Error converting %%f
    ) else (
        echo Successfully converted %%f to %%~nf.webp
    )
)

echo Conversion complete.
endlocal
pause
