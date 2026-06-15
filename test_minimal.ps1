Add-Type @"
using System;
using System.Windows.Forms;
public class TestForm : Form {
    public TestForm() {
        Text = "Test";
        Size = new System.Drawing.Size(800, 600);
        var btn = new Button { Text = "Click Me", Location = new System.Drawing.Point(10, 10), Size = new System.Drawing.Size(100, 30) };
        btn.Click += (s, e) => MessageBox.Show("Working!");
        Controls.Add(btn);
    }
}
"@ -ReferencedAssemblies "System.Windows.Forms","System.Drawing"

Write-Host "Starting test form..." -ForegroundColor Cyan
$form = New-Object TestForm
$form.Show()
Start-Sleep -Seconds 3
Write-Host "Form title: $($form.Text)" -ForegroundColor Green
Write-Host "Form visible: $($form.Visible)" -ForegroundColor Green
Write-Host "Form responding: $(![System.Windows.Forms.Application]::MessageLoop)" -ForegroundColor Yellow
$form.Close()
Write-Host "Test done" -ForegroundColor Cyan
