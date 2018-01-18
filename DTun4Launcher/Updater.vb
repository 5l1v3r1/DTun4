Imports System.Net
Imports System.IO

Public Class Updater
    Dim cl As WebClient = New WebClient
    Private Sub Updater_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12
        If Command$().ToLower.Contains("-sr") Then
            Threading.Thread.Sleep(2000)
            DlAndStart()
        End If
        If File.Exists(My.Application.Info.DirectoryPath & "\DTun4ClientLibrary.dll") Then

            Dim fvi As FileVersionInfo = FileVersionInfo.GetVersionInfo(My.Application.Info.DirectoryPath & "\DTun4ClientLibrary.dll")
            Dim fv = fvi.FileMajorPart & fvi.FileMinorPart
            Dim ok As String = ""
            Try
                ok = cl.DownloadString("https://dtun4.disahome.me/data/check.php?version=" & fv)
            Catch
                Shell(".\DTun4.exe -updated")
                Environment.Exit(0)
            End Try
            If ok.Contains("OK") Then
                Shell(".\DTun4.exe -updated")
                Environment.Exit(0)
            ElseIf ok.Contains("BAD") Then
                DlAndStart()
            ElseIf ok.Contains("MESS ") Then
                MessageBox.Show(ok.Replace("MESS ", ""))
                Environment.Exit(1)
            End If
        Else
            DlAndStart()
        End If
    End Sub
    Sub DlAndStart()
        cl.DownloadFile(New Uri("https://dtun4.disahome.me/dl/DTun4ClientLibrary.dll"), "DTun4ClientLibrary.dll")
        cl.DownloadFile(New Uri("https://dtun4.disahome.me/dl/DTun4.exe"), "DTun4.exe")
        Shell(".\DTun4.exe -updated")
        Environment.Exit(0)
    End Sub

End Class
