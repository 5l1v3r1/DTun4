Class Application
    Private Sub Application_Startup(sender As Object, e As System.Windows.StartupEventArgs) Handles Me.Startup
        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf KaboomHandler
    End Sub
    Private Sub KaboomHandler(sender As Object, e As UnhandledExceptionEventArgs)
        'MsgBox(e.ExceptionObject.ToString())
        MessageBox.Show(e.ExceptionObject.ToString)

    End Sub
    ' Application-level events, such as Startup, Exit, and DispatcherUnhandledException
    ' can be handled in this file.

End Class
