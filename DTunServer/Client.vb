Imports System.Net

Class Client
    Private ipadd As String
    Private ep As IPEndPoint
    Private nname As String
    Private net As String
    Private timeout As Integer
    Private aes() As Byte
    ReadOnly Property Key As String
        Get
            Return System.Text.Encoding.Default.GetString(aes)
        End Get
    End Property
    Property IP() As String
        Get
            Return ipadd
        End Get
        Set(value As String)
            ipadd = value
        End Set
    End Property
    Property EndP() As IPEndPoint
        Get
            Return ep
        End Get
        Set(value As IPEndPoint)
            ep = value
        End Set
    End Property
    Property Name() As String
        Get
            Return nname
        End Get
        Set(value As String)
            nname = value
        End Set
    End Property
    Property Network() As String
        Get
            Return net
        End Get
        Set(value As String)
            net = value
        End Set
    End Property
    Property Time() As Integer
        Get
            Return timeout
        End Get
        Set(value As Integer)
            timeout = value
        End Set
    End Property
    Public Function Encrypt(str As Byte()) As Byte()
        Return AES_Encrypt(str, aes)
    End Function
    Public Function Decrypt(str As Byte()) As Byte()
        Return AES_Decrypt(str, aes)
    End Function

    Sub New(a1 As String, a2 As IPEndPoint, a3 As String, a4 As String, AESKEY As Byte())
        ipadd = a1
        ep = a2
        nname = a3
        net = a4
        timeout = 30
        aes = AESKEY
    End Sub
End Class