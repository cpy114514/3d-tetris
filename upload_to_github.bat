@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

cd /d "%~dp0"

echo.
echo === Upload 3D Tetris to GitHub ===
echo Project: %cd%
echo.

where git >nul 2>nul
if errorlevel 1 (
    echo ERROR: Git is not installed or not in PATH.
    pause
    exit /b 1
)

git rev-parse --is-inside-work-tree >nul 2>nul
if errorlevel 1 (
    echo ERROR: This folder is not a git repository.
    pause
    exit /b 1
)

git remote get-url origin >nul 2>nul
if errorlevel 1 (
    echo ERROR: No GitHub remote named origin is configured.
    pause
    exit /b 1
)

for /f "delims=" %%b in ('git branch --show-current') do set "BRANCH=%%b"
if "%BRANCH%"=="" set "BRANCH=main"

echo Current branch: %BRANCH%
echo Remote:
git remote -v
echo.

echo Local changes:
git status --short
echo.

for /f %%c in ('git status --porcelain ^| findstr /v /i /c:" README.md" ^| find /c /v ""') do set "CHANGE_COUNT=%%c"

if "%CHANGE_COUNT%"=="0" (
    echo No local changes to commit outside README.md. Pushing current branch only...
    git push -u origin "%BRANCH%"
    if errorlevel 1 goto failed
    goto done
)

set "COMMIT_MSG=%*"
if "%COMMIT_MSG%"=="" (
    set /p "COMMIT_MSG=Commit message (press Enter for default): "
)

if "%COMMIT_MSG%"=="" set "COMMIT_MSG=Update Unity project"

echo.
echo Staging changes...
git add -A
if errorlevel 1 goto failed
git reset -q HEAD -- README.md 2>nul
echo README.md is skipped by this upload script.

git diff --cached --quiet
if not errorlevel 1 (
    echo Nothing was staged. Pushing current branch only...
    git push -u origin "%BRANCH%"
    if errorlevel 1 goto failed
    goto done
)

echo.
echo Committing: %COMMIT_MSG%
git commit -m "%COMMIT_MSG%"
if errorlevel 1 goto failed

echo.
echo Pushing to GitHub...
git push -u origin "%BRANCH%"
if errorlevel 1 goto failed

:done
echo.
echo Upload complete.
pause
exit /b 0

:failed
echo.
echo Upload failed. Check the error above.
pause
exit /b 1
