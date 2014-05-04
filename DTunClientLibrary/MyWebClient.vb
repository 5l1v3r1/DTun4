Imports System.Net

Class MyWebClient
    Inherits WebClient

    Protected Overrides Function GetWebRequest(uri As Uri) As WebRequest

        Dim w As WebRequest = MyBase.GetWebRequest(uri)
        w.Timeout = 6000
        Return w
    End Function
End Class