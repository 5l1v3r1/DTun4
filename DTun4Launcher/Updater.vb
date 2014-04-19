Imports System.Net
Imports System.IO

Public Class Updater

    Private Sub Updater_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim cl As WebClient = New WebClient

        Dim fvi As FileVersionInfo = FileVersionInfo.GetVersionInfo(My.Application.Info.DirectoryPath & "\DTun4ClientLibrary.dll")
        Dim fv = fvi.FileMajorPart & fvi.FileMinorPart
        Dim ok As String = ""
        Try
            ok = cl.DownloadString("http://dtun4.disahome.tk/data/check.php?version=" & fv)
        Catch
            Shell(".\DTun4.exe -updated")
            Environment.Exit(0)
        End Try
        If ok.Contains("OK") Then
            Shell(".\DTun4.exe -updated")
            Environment.Exit(0)
        ElseIf ok.Contains("BAD") Then
            'AddHandler cl.DownloadFileCompleted, AddressOf done
            cl.DownloadFile(New Uri("http://dtun4.disahome.tk/dl/DTun4ClientLibrary.dll"), "DTun4ClientLibrary.dll")
            cl.DownloadFile(New Uri("http://dtun4.disahome.tk/dl/DTun4.exe"), "DTun4.exe")
            Shell(".\DTun4.exe -updated")
            Environment.Exit(0)
        ElseIf ok.Contains("MESS ") Then
            MessageBox.Show(ok.Replace("MESS ", ""))
            Environment.Exit(1)
        End If
    End Sub
End Class
