@echo off
REM ============================================================
REM BR-DL分割ツール - Visual Basic EXEコンパイラ
REM ============================================================

setlocal enabledelayedexpansion

echo.
echo ======================================
echo BR-DL分割ツール - EXEコンパイル
echo ======================================
echo.

REM Visual Studioのパスを検索
set "VB_COMPILER="
set "VS_PATH="

REM Visual Studio 2022
if exist "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\vbc.exe" (
    set "VB_COMPILER=C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\vbc.exe"
    set "VS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Community"
    goto :found_vs
)

if exist "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\vbc.exe" (
    set "VB_COMPILER=C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\vbc.exe"
    set "VS_PATH=C:\Program Files\Microsoft Visual Studio\2022\Professional"
    goto :found_vs
)

REM Visual Studio 2019
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\vbc.exe" (
    set "VB_COMPILER=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\vbc.exe"
    set "VS_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2019\Community"
    goto :found_vs
)

REM Visual Studio 2017
if exist "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\Roslyn\vbc.exe" (
    set "VB_COMPILER=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\Roslyn\vbc.exe"
    set "VS_PATH=C:\Program Files (x86)\Microsoft Visual Studio\2017\Community"
    goto :found_vs
)

REM VBコンパイラが見つからない場合
echo エラー: Visual Basicコンパイラが見つかりません
echo.
echo 解決方法：
echo 1. Visual Studio Community版をインストール
echo    https://visualstudio.microsoft.com/ja/vs/community/
echo.
echo 2. インストール時に「.NET デスクトップ開発」を選択
echo.
echo 3. インストール完了後、このスクリプトを再度実行
echo.
pause
exit /b 1

:found_vs
echo ✓ コンパイラが見つかりました
echo   パス: %VB_COMPILER%
echo.

echo.
echo コンパイル中...
echo.

REM VB.NETファイルをコンパイル（デバッグ情報なし、最適化あり）
"%VB_COMPILER%" /rootnamespace:FileSplitterApp ^
    /target:winexe ^
    /out:FileSplitter.exe ^
    /optimize+ ^
    /debug- ^
    /reference:System.Windows.Forms.dll ^
    /reference:System.Drawing.dll ^
    /reference:System.Text.Json.dll ^
    FileSplitter.vb

if %errorlevel% equ 0 (
    echo.
    echo ✓ コンパイル成功！
    echo.
    echo ==========================================
    echo FileSplitter.exe が作成されました
    echo ==========================================
    echo.
    echo • ファイルサイズ: 
    for %%A in (FileSplitter.exe) do echo   %%~zA バイト
    echo.
    echo 実行方法:
    echo  1. FileSplitter.exe をダブルクリック
    echo  2. または、コマンドラインで実行: FileSplitter.exe
    echo.
    pause
    
    REM EXEを実行するか確認
    setlocal
    choice /C YN /M "今すぐアプリケーションを起動しますか？"
    if errorlevel 2 goto :eof
    if errorlevel 1 (
        start FileSplitter.exe
        goto :eof
    )
) else (
    echo.
    echo ✕ コンパイルに失敗しました
    echo.
    echo エラーの詳細については上記を確認してください
    echo.
    pause
    exit /b 1
)

:eof
endlocal
