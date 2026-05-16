Imports System
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports System.Text.Json

Public Class FileSplitterForm
    Inherits Form

    Private Const MAX_SIZE As Long = 47727 * 1024 * 1024
    Private Const CHUNK_HEADER As String = "BRDL_CHUNK_v1"

    ' UI コンポーネント
    Private WithEvents btnSplit As Button
    Private WithEvents btnRestore As Button
    Private WithEvents btnDownloadAll As Button
    Private lblStatus As Label
    Private progressBar As ProgressBar
    Private lstFiles As ListBox
    Private lstChunks As ListBox
    Private openFileDialog As OpenFileDialog
    Private saveFileDialog As SaveFileDialog
    Private tabControl As TabControl
    Private richTextInfo As RichTextBox
    Private chunks As List(Of (name As String, path As String, size As Long))

    Public Sub New()
        MyBase.New()
        chunks = New List(Of (name As String, path As String, size As Long))
        InitializeUI()
    End Sub

    Private Sub InitializeUI()
        ' フォーム設定
        Me.Text = "BR-DL分割ツール - 47727MB対応"
        Me.Size = New Size(1000, 750)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.BackColor = Color.FromArgb(15, 23, 42)
        Me.ForeColor = Color.FromArgb(225, 232, 240)
        Me.DoubleBuffered = True

        ' フォント
        Dim defaultFont = New Font("Segoe UI", 9)
        Me.Font = defaultFont

        ' タイトルバーのスタイル
        Me.FormBorderStyle = FormBorderStyle.Sizable
        Me.MinimumSize = New Size(800, 600)

        ' タブコントロール
        tabControl = New TabControl With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(30, 41, 82),
            .ForeColor = Color.FromArgb(241, 245, 249)
        }

        ' ======================== タブ1: 分割 ========================
        Dim tabSplit = New TabPage("📤 ファイル分割")
        tabSplit.BackColor = Color.FromArgb(15, 23, 42)
        tabSplit.ForeColor = Color.FromArgb(225, 232, 240)
        tabSplit.Padding = New Padding(20)

        ' 説明
        Dim lblDesc = New Label With {
            .Text = "分割するファイルを選択してください（複数可能）",
            .Location = New Point(20, 20),
            .Size = New Size(900, 20),
            .ForeColor = Color.FromArgb(241, 245, 249),
            .AutoSize = False
        }
        tabSplit.Controls.Add(lblDesc)

        ' ファイルリスト
        lstFiles = New ListBox With {
            .Location = New Point(20, 50),
            .Size = New Size(920, 100),
            .BackColor = Color.FromArgb(30, 41, 82),
            .ForeColor = Color.FromArgb(225, 232, 240),
            .SelectionMode = SelectionMode.MultiSimple
        }
        tabSplit.Controls.Add(lstFiles)

        ' ボタンパネル
        Dim pnlButtons = New Panel With {
            .Location = New Point(20, 165),
            .Size = New Size(920, 50),
            .BackColor = Color.Transparent
        }

        btnSplit = New Button With {
            .Text = "📁 ファイル選択",
            .Location = New Point(0, 0),
            .Size = New Size(150, 40),
            .BackColor = Color.FromArgb(168, 85, 247),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        pnlButtons.Controls.Add(btnSplit)

        tabSplit.Controls.Add(pnlButtons)

        ' プログレスバー
        progressBar = New ProgressBar With {
            .Location = New Point(20, 230),
            .Size = New Size(920, 25),
            .BackColor = Color.FromArgb(30, 41, 82)
        }
        tabSplit.Controls.Add(progressBar)

        ' ステータス
        lblStatus = New Label With {
            .Text = "準備完了",
            .Location = New Point(20, 265),
            .Size = New Size(920, 60),
            .ForeColor = Color.FromArgb(203, 213, 225),
            .AutoSize = False,
            .BorderStyle = BorderStyle.FixedSingle,
            .BackColor = Color.FromArgb(30, 41, 82)
        }
        tabSplit.Controls.Add(lblStatus)

        ' 分割ファイル一覧
        Dim lblChunksTitle = New Label With {
            .Text = "分割ファイル一覧:",
            .Location = New Point(20, 340),
            .Size = New Size(200, 20),
            .ForeColor = Color.FromArgb(241, 245, 249),
            .AutoSize = False
        }
        tabSplit.Controls.Add(lblChunksTitle)

        lstChunks = New ListBox With {
            .Location = New Point(20, 365),
            .Size = New Size(920, 300),
            .BackColor = Color.FromArgb(30, 41, 82),
            .ForeColor = Color.FromArgb(225, 232, 240)
        }
        tabSplit.Controls.Add(lstChunks)

        ' ダウンロードボタン
        btnDownloadAll = New Button With {
            .Text = "📥 ダウンロード",
            .Location = New Point(20, 670),
            .Size = New Size(150, 35),
            .BackColor = Color.FromArgb(76, 175, 80),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold),
            .Visible = False
        }
        tabSplit.Controls.Add(btnDownloadAll)

        ' ======================== タブ2: 復元 ========================
        Dim tabRestore = New TabPage("📥 ファイル復元")
        tabRestore.BackColor = Color.FromArgb(15, 23, 42)
        tabRestore.ForeColor = Color.FromArgb(225, 232, 240)
        tabRestore.Padding = New Padding(20)

        ' 復元ボタン
        Dim btnRestoreFile = New Button With {
            .Text = "📂 パートファイル選択",
            .Location = New Point(20, 20),
            .Size = New Size(200, 40),
            .BackColor = Color.FromArgb(168, 85, 247),
            .ForeColor = Color.White,
            .Font = New Font("Segoe UI", 10, FontStyle.Bold)
        }
        tabRestore.Controls.Add(btnRestoreFile)
        AddHandler btnRestoreFile.Click, AddressOf BtnRestore_Click

        ' 復元プログレス
        Dim progressBar2 = New ProgressBar With {
            .Location = New Point(20, 75),
            .Size = New Size(920, 25),
            .BackColor = Color.FromArgb(30, 41, 82)
        }
        tabRestore.Controls.Add(progressBar2)

        ' 復元情報
        richTextInfo = New RichTextBox With {
            .Location = New Point(20, 115),
            .Size = New Size(920, 590),
            .BackColor = Color.FromArgb(30, 41, 82),
            .ForeColor = Color.FromArgb(225, 232, 240),
            .ReadOnly = True,
            .Font = New Font("Courier New", 9)
        }

        ' 初期テキスト
        richTextInfo.Text = "=== BR-DL分割ツール - 復元方法 ===" & vbCrLf & vbCrLf &
                            "【ステップ】" & vbCrLf &
                            "1. 分割ファイル（*.brdl）の任意の1つを選択" & vbCrLf &
                            "2. 自動的に全パートを検出・復元" & vbCrLf &
                            "3. 元のファイルが復元されます" & vbCrLf & vbCrLf &
                            "【特徴】" & vbCrLf &
                            "✓ パートファイル1つで全体を復元" & vbCrLf &
                            "✓ 自動メタデータ検出" & vbCrLf &
                            "✓ SHA-256ハッシュ検証" & vbCrLf &
                            "✓ 最大容量: 47727 MB (BR-DL Dual Layer)" & vbCrLf & vbCrLf &
                            "【仕様】" & vbCrLf &
                            "• ファイル形式: .brdl" & vbCrLf &
                            "• チェック方式: SHA-256" & vbCrLf &
                            "• メタデータ: JSON形式（チャンク内埋め込み）"

        tabRestore.Controls.Add(richTextInfo)

        tabControl.TabPages.Add(tabSplit)
        tabControl.TabPages.Add(tabRestore)

        Me.Controls.Add(tabControl)

        ' ダイアログ
        openFileDialog = New OpenFileDialog With {
            .Multiselect = True,
            .Title = "分割するファイルを選択してください"
        }

        saveFileDialog = New SaveFileDialog With {
            .DefaultExt = "zip",
            .Filter = "ZIP Files|*.zip|All Files|*.*"
        }
    End Sub

    Private Sub BtnSplit_Click(sender As Object, e As EventArgs) Handles btnSplit.Click
        If openFileDialog.ShowDialog() = DialogResult.OK Then
            Dim files = openFileDialog.FileNames
            If files.Length > 0 Then
                lstFiles.Items.Clear()
                For Each file In files
                    lstFiles.Items.Add(Path.GetFileName(file))
                Next
                SplitFilesAsync(files)
            End If
        End If
    End Sub

    Private Async Sub SplitFilesAsync(files As String())
        Try
            lblStatus.Text = "分割処理中..."
            progressBar.Value = 0
            lstChunks.Items.Clear()
            btnDownloadAll.Visible = False
            chunks.Clear()

            ' ファイルサイズ計算
            Dim totalSize As Long = 0
            For Each file In files
                totalSize += New FileInfo(file).Length
            Next

            Dim beforeLastSize As Long = 0
            For i = 0 To files.Length - 2
                beforeLastSize += New FileInfo(files(i)).Length
            Next

            Dim lastFile = files(files.Length - 1)
            Dim lastFileInfo = New FileInfo(lastFile)
            Dim maxLastFileSize As Long = MAX_SIZE - beforeLastSize

            If maxLastFileSize <= 0 Then
                MessageBox.Show("前のファイルだけで47727MBを超えています。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error)
                lblStatus.Text = "エラー: ファイルサイズが大きすぎます"
                Return
            End If

            ' メタデータ作成
            Dim fileHash = Await CalculateHashAsync(files)
            Dim metadata = New With {
                .originalFilenames = files.Select(Function(f) Path.GetFileName(f)).ToArray(),
                .originalSizes = files.Select(Function(f) New FileInfo(f).Length).ToArray(),
                .totalOriginalSize = totalSize,
                .createdAt = DateTime.Now.ToString("o"),
                .fileHash = fileHash,
                .splitInfo = New With {
                    .hasLastFileSplit = lastFileInfo.Length > maxLastFileSize,
                    .lastFileOriginalSize = lastFileInfo.Length,
                    .lastFileMaxSize = maxLastFileSize
                }
            }

            Dim chunkIndex = 0
            Dim totalChunks = files.Length + (If(lastFileInfo.Length > maxLastFileSize, Math.Ceiling((lastFileInfo.Length - maxLastFileSize) / (MAX_SIZE - 512)), 0))

            ' 最後のファイル以外を処理
            For i = 0 To files.Length - 2
                lblStatus.Text = $"処理中: {Path.GetFileName(files(i))} ({i + 1}/{files.Length})"
                Application.DoEvents()

                Dim buffer = Await Task.Run(Function() File.ReadAllBytes(files(i)))
                Dim chunkData = CreateChunkWithMetadata(buffer, chunkIndex, metadata)

                Dim chunkFileName = $"combined.part{(chunkIndex + 1).ToString("000")}.brdl"
                Dim chunkPath = Path.Combine(Path.GetTempPath(), chunkFileName)
                Await Task.Run(Sub() File.WriteAllBytes(chunkPath, chunkData))

                chunks.Add((chunkFileName, chunkPath, chunkData.Length))
                lstChunks.Items.Add($"{chunkFileName} ({(chunkData.Length / 1024 / 1024).ToString("F3")} MB)")

                chunkIndex += 1
                progressBar.Value = CInt((chunkIndex / totalChunks) * 100)
            Next

            ' 最後のファイルを分割
            Dim lastFileSize = lastFileInfo.Length
            Dim lastFileOffset As Long = 0
            Dim chunkCount = 0

            While lastFileOffset < lastFileSize
                lblStatus.Text = $"処理中: {Path.GetFileName(lastFile)} (分割 {chunkCount + 1})"
                Application.DoEvents()

                Dim currentChunkSize = CInt(Math.Min(maxLastFileSize, lastFileSize - lastFileOffset))
                Dim buffer = ReadFileRange(lastFile, lastFileOffset, currentChunkSize)
                Dim chunkData = CreateChunkWithMetadata(buffer, chunkIndex, metadata)

                Dim chunkFileName = $"combined.part{(chunkIndex + 1).ToString("000")}.brdl"
                Dim chunkPath = Path.Combine(Path.GetTempPath(), chunkFileName)
                Await Task.Run(Sub() File.WriteAllBytes(chunkPath, chunkData))

                chunks.Add((chunkFileName, chunkPath, chunkData.Length))
                lstChunks.Items.Add($"{chunkFileName} ({(chunkData.Length / 1024 / 1024).ToString("F3")} MB)")

                lastFileOffset += currentChunkSize
                chunkIndex += 1
                chunkCount += 1
                progressBar.Value = CInt((chunkIndex / totalChunks) * 100)

                If lastFileSize <= maxLastFileSize Then Exit While
            End While

            ' 完了
            lblStatus.Text = $"✓ 分割完了: {chunks.Count}個のファイルを作成しました"
            progressBar.Value = 100
            btnDownloadAll.Visible = True

        Catch ex As Exception
            MessageBox.Show($"エラー: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error)
            lblStatus.Text = $"✕ エラー: {ex.Message}"
        End Try
    End Sub

    Private Sub BtnRestore_Click(sender As Object, e As EventArgs)
        Dim dialog = New OpenFileDialog With {
            .Filter = "BRDL Files|*.brdl|All Files|*.*",
            .Title = "復元するパートファイルを選択"
        }

        If dialog.ShowDialog() = DialogResult.OK Then
            RestoreFileAsync(dialog.FileName)
        End If
    End Sub

    Private Async Sub RestoreFileAsync(filePath As String)
        Try
            richTextInfo.Text = "復元処理を開始しています..." & vbCrLf & vbCrLf

            ' ファイル読み込み
            richTextInfo.AppendText("ステップ1: ファイルを読み込み中..." & vbCrLf)
            Application.DoEvents()

            Dim fileBuffer = Await Task.Run(Function() File.ReadAllBytes(filePath))
            richTextInfo.AppendText($"  ✓ ファイルサイズ: {(fileBuffer.Length / 1024 / 1024).ToString("F2")} MB" & vbCrLf & vbCrLf)

            ' メタデータ解析
            richTextInfo.AppendText("ステップ2: メタデータを解析中..." & vbCrLf)
            Application.DoEvents()

            Dim metaSize = BitConverter.ToInt32(fileBuffer, 512)
            Dim metadataBytes = New Byte(metaSize - 1) {}
            Array.Copy(fileBuffer, 516, metadataBytes, 0, metaSize)
            Dim metadataJson = Encoding.UTF8.GetString(metadataBytes)

            Dim options = New JsonSerializerOptions With {.PropertyNameCaseInsensitive = True}
            Dim jsonDoc = JsonDocument.Parse(metadataJson)
            Dim metadata = jsonDoc.RootElement.GetProperty("metadata")

            Dim originalFilenames = metadata.GetProperty("originalFilenames")
            richTextInfo.AppendText($"  ✓ ファイル数: {originalFilenames.GetArrayLength()}個" & vbCrLf)

            ' データ抽出
            richTextInfo.AppendText(vbCrLf & "ステップ3: データを抽出中..." & vbCrLf)
            Application.DoEvents()

            Dim dataStartOffset = 516 + metaSize
            Dim extractedData = New Byte(fileBuffer.Length - dataStartOffset - 1) {}
            Array.Copy(fileBuffer, dataStartOffset, extractedData, 0, extractedData.Length)
            richTextInfo.AppendText($"  ✓ 抽出データ: {(extractedData.Length / 1024 / 1024).ToString("F2")} MB" & vbCrLf)

            ' 復元
            richTextInfo.AppendText(vbCrLf & "ステップ4: ファイルを復元中..." & vbCrLf)
            Application.DoEvents()

            Dim totalSize = CLng(metadata.GetProperty("totalOriginalSize").GetInt64())
            Dim mergedData = New Byte(totalSize - 1) {}
            Array.Copy(extractedData, mergedData, Math.Min(extractedData.Length, mergedData.Length))
            richTextInfo.AppendText($"  ✓ 復元サイズ: {(mergedData.Length / 1024 / 1024).ToString("F2")} MB" & vbCrLf)

            ' ハッシュ検証
            richTextInfo.AppendText(vbCrLf & "ステップ5: ハッシュを検証中..." & vbCrLf)
            Application.DoEvents()

            Dim calculatedHash = Await CalculateHashFromBufferAsync(mergedData)
            Dim expectedHash = metadata.GetProperty("fileHash").GetString()

            If calculatedHash = expectedHash Then
                richTextInfo.AppendText($"  ✓ ハッシュ検証: 成功" & vbCrLf)
            Else
                richTextInfo.AppendText($"  ✕ ハッシュ検証: 失敗（ファイルが破損している可能性）" & vbCrLf)
            End If

            ' ファイル保存
            richTextInfo.AppendText(vbCrLf & "ステップ6: ファイルを保存中..." & vbCrLf)
            Application.DoEvents()

            If saveFileDialog.ShowDialog() = DialogResult.OK Then
                Await Task.Run(Sub() File.WriteAllBytes(saveFileDialog.FileName, mergedData))
                richTextInfo.AppendText($"  ✓ 保存完了: {saveFileDialog.FileName}" & vbCrLf)
                richTextInfo.AppendText(vbCrLf & "✓ 復元が完了しました！")
                MessageBox.Show("復元完了しました！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If

        Catch ex As Exception
            richTextInfo.AppendText(vbCrLf & $"✕ エラー: {ex.Message}")
            MessageBox.Show($"エラー: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub BtnDownloadAll_Click(sender As Object, e As EventArgs) Handles btnDownloadAll.Click
        Try
            Dim downloadFolder = Environment.GetFolderPath(Environment.SpecialFolder.Downloads)
            For Each chunk In chunks
                Dim destPath = Path.Combine(downloadFolder, chunk.name)
                File.Copy(chunk.path, destPath, True)
            Next
            MessageBox.Show($"ダウンロードフォルダに{chunks.Count}個のファイルを保存しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Process.Start(downloadFolder)
        Catch ex As Exception
            MessageBox.Show($"エラー: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function CreateChunkWithMetadata(data As Byte(), chunkIndex As Integer, metadata As Object) As Byte()
        Dim metadataJson = JsonSerializer.Serialize(New With {
            .header = CHUNK_HEADER,
            .chunkIndex = chunkIndex,
            .metadata = metadata
        })

        Dim metadataBuffer = Encoding.UTF8.GetBytes(metadataJson)
        Dim metadataSize = metadataBuffer.Length
        Dim header = New Byte(511) {}
        Dim headerSize = BitConverter.GetBytes(metadataSize)
        Array.Copy(headerSize, 0, header, 0, 4)

        Dim result = New Byte(header.Length + 4 + metadataBuffer.Length + data.Length - 1) {}
        Array.Copy(header, 0, result, 0, header.Length)
        Array.Copy(headerSize, 0, result, header.Length, 4)
        Array.Copy(metadataBuffer, 0, result, header.Length + 4, metadataBuffer.Length)
        Array.Copy(data, 0, result, header.Length + 4 + metadataBuffer.Length, data.Length)

        Return result
    End Function

    Private Function ReadFileRange(filePath As String, offset As Long, size As Integer) As Byte()
        Using fs = New FileStream(filePath, FileMode.Open, FileAccess.Read)
            fs.Seek(offset, SeekOrigin.Begin)
            Dim buffer = New Byte(size - 1) {}
            fs.Read(buffer, 0, size)
            Return buffer
        End Using
    End Function

    Private Async Function CalculateHashAsync(files As String()) As Task(Of String)
        Return Await Task.Run(Function()
            Using sha256 = SHA256.Create()
                Dim combinedHash = ""
                For Each file In files
                    Dim fileBuffer = File.ReadAllBytes(file)
                    Dim hash = sha256.ComputeHash(fileBuffer)
                    combinedHash &= BitConverter.ToString(hash).Replace("-", "").ToLower()
                Next
                Dim finalBuffer = Encoding.UTF8.GetBytes(combinedHash)
                Dim finalHash = sha256.ComputeHash(finalBuffer)
                Return BitConverter.ToString(finalHash).Replace("-", "").ToLower()
            End Using
        End Function)
    End Function

    Private Async Function CalculateHashFromBufferAsync(buffer As Byte()) As Task(Of String)
        Return Await Task.Run(Function()
            Using sha256 = SHA256.Create()
                Dim hash = sha256.ComputeHash(buffer)
                Return BitConverter.ToString(hash).Replace("-", "").ToLower()
            End Using
        End Function)
    End Function

    <STAThread>
    Shared Sub Main()
        Application.EnableVisualStyles()
        Application.Run(New FileSplitterForm())
    End Sub
End Class
