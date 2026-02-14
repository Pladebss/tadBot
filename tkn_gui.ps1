Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$runBat = Join-Path $root "run.bat"

if (-not (Test-Path $runBat)) {
  [System.Windows.Forms.MessageBox]::Show("run.bat not found in:`n$root", "TadBot Token Editor", "OK", "Error") | Out-Null
  exit 1
}

# --- UI ---
$form = New-Object System.Windows.Forms.Form
$form.Text = "TadBot Token Editor"
$form.StartPosition = "CenterScreen"
$form.Size = New-Object System.Drawing.Size(520, 220)
$form.MaximizeBox = $false
$form.FormBorderStyle = "FixedDialog"
$form.TopMost = $true

$label = New-Object System.Windows.Forms.Label
$label.Text = "Paste your Discord bot token:"
$label.AutoSize = $true
$label.Location = New-Object System.Drawing.Point(14, 18)
$form.Controls.Add($label)

$text = New-Object System.Windows.Forms.TextBox
$text.Location = New-Object System.Drawing.Point(17, 45)
$text.Size = New-Object System.Drawing.Size(470, 25)
$text.Anchor = "Top,Left,Right"
$text.UseSystemPasswordChar = $false  # set $true if you want it hidden
$form.Controls.Add($text)

$hint = New-Object System.Windows.Forms.Label
$hint.Text = 'This will update the line: set "DISCORD_BOT_TOKEN=..." inside run.bat'
$hint.AutoSize = $true
$hint.Location = New-Object System.Drawing.Point(14, 78)
$form.Controls.Add($hint)

$status = New-Object System.Windows.Forms.Label
$status.Text = ""
$status.AutoSize = $true
$status.ForeColor = [System.Drawing.Color]::DarkRed
$status.Location = New-Object System.Drawing.Point(14, 105)
$form.Controls.Add($status)

$btnSave = New-Object System.Windows.Forms.Button
$btnSave.Text = "Save"
$btnSave.Location = New-Object System.Drawing.Point(317, 135)
$btnSave.Size = New-Object System.Drawing.Size(80, 30)
$form.Controls.Add($btnSave)

$btnCancel = New-Object System.Windows.Forms.Button
$btnCancel.Text = "Cancel"
$btnCancel.Location = New-Object System.Drawing.Point(407, 135)
$btnCancel.Size = New-Object System.Drawing.Size(80, 30)
$form.Controls.Add($btnCancel)

$btnCancel.Add_Click({ $form.Close() })

function Update-RunBatToken([string]$token) {
  if ([string]::IsNullOrWhiteSpace($token)) { return "Token cannot be empty." }
  if ($token -match "PASTE_TOKEN_HERE") { return "You didn't paste a real token." }

  $lines = Get-Content -LiteralPath $runBat -ErrorAction Stop

  $found = $false
  $out = foreach ($line in $lines) {
    if ($line -match '^set\s+"DISCORD_BOT_TOKEN=') {
      $found = $true
      'set "DISCORD_BOT_TOKEN=' + $token + '"'
    } else {
      $line
    }
  }

  if (-not $found) {
    return "Could not find token line in run.bat. Expected: set ""DISCORD_BOT_TOKEN=..."""
  }

  Set-Content -LiteralPath $runBat -Value $out -Encoding ASCII
  return $null
}

$btnSave.Add_Click({
  $status.Text = ""
  $err = Update-RunBatToken $text.Text.Trim()
  if ($err) {
    $status.ForeColor = [System.Drawing.Color]::DarkRed
    $status.Text = $err
  } else {
    [System.Windows.Forms.MessageBox]::Show("Token updated successfully.", "TadBot Token Editor", "OK", "Information") | Out-Null
    $form.Close()
  }
})

$form.Add_Shown({ $text.Focus() })
[void]$form.ShowDialog()
