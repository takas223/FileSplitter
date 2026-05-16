# BR-DL分割ツール - PowerShellコンパイラ

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "BR-DL分割ツール - EXEコンパイル" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Visual Studioのパスを検索
$VBCompiler = $null
$VSPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\vbc.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\vbc.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\vbc.exe",
    "C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\Roslyn\vbc.exe"
)

foreach ($path in $VSPaths) {
    if (Test-Path $path) {
        $VBCompiler = $path
        Write-Host "✓ コンパイラが見つかりました" -ForegroundColor Green
        Write-Host "  パス: $VBCompiler" -ForegroundColor Gray
        break
    }
}

if ($null -eq $VBCompiler) {
    Write-Host "エラー: Visual Basicコンパイラが見つかりません" -ForegroundColor Red
    Write-Host ""
    Write-Host "解決方法:" -ForegroundColor Yellow
    Write-Host "1. Visual Studio Community版をインストール"
    Write-Host "   https://visualstudio.microsoft.com/ja/vs/community/"
    Write-Host ""
    Write-Host "2. インストール時に『.NET デスクトップ開発』を選択"
    Write-Host ""
    Write-Host "3. インストール完了後、このスクリプトを再度実行"
    Read-Host "Enterキーを押してください"
    exit 1
}

Write-Host ""
Write-Host "コンパイル中..." -ForegroundColor Yellow
Write-Host ""

# VB.NETファイルをコンパイル
$SourceFile = "FileSplitter.vb"
$OutputFile = "FileSplitter.exe"

if (-not (Test-Path $SourceFile)) {
    Write-Host "エラー: $SourceFile が見つかりません" -ForegroundColor Red
    exit 1
}

# コンパイルコマンドを実行（デバッグ情報なし、最適化あり）
& $VBCompiler `
    /rootnamespace:FileSplitterApp `
    /target:winexe `
    /out:$OutputFile `
    /optimize+ `
    /debug- `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:System.Text.Json.dll `
    $SourceFile

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "✓ コンパイル成功！" -ForegroundColor Green
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host "$OutputFile が作成されました" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Green
    Write-Host ""
    
    $FileSize = (Get-Item $OutputFile).Length
    Write-Host "• ファイルサイズ: $FileSize バイト"
    Write-Host ""
    Write-Host "実行方法:" -ForegroundColor Yellow
    Write-Host "  1. $OutputFile をダブルクリック"
    Write-Host "  2. または、PowerShellで実行: .\$OutputFile"
    Write-Host ""
    
    $response = Read-Host "今すぐアプリケーションを起動しますか？ (Y/N)"
    if ($response -eq "Y" -or $response -eq "y") {
        & "./$OutputFile"
    }
} else {
    Write-Host ""
    Write-Host "✕ コンパイルに失敗しました" -ForegroundColor Red
    Write-Host ""
    Write-Host "エラーの詳細については上記を確認してください" -ForegroundColor Yellow
    exit 1
}
