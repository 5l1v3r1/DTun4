
Imports System.Collections.Generic
Imports System.Text
Imports System.Net
Imports System.Net.Sockets
Imports System.IO

' ''' <summary>
' ''' This class implements STUN client. Defined in RFC 3489.
' ''' </summary>
' ''' <example>
' ''' <code>
' ''' // Create new socket for STUN client.
' ''' Socket socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
' ''' socket.Bind(new IPEndPoint(IPAddress.Any,0));
' ''' 
' ''' // Query STUN server
' ''' STUN_Result result = STUN_Client.Query("stunserver.org",3478,socket);
' ''' if(result.NetType != STUN_NetType.UdpBlocked){
' '''     // UDP blocked or !!!! bad STUN server
' ''' }
' ''' else{
' '''     IPEndPoint publicEP = result.PublicEndPoint;
' '''     // Do your stuff
' ''' }
' ''' </code>
' ''' </example>
'Public Class STUN_Client
'#Region "static method Query"

'    ''' <summary>
'    ''' Gets NAT info from STUN server.
'    ''' </summary>
'    ''' <param name="host">STUN server name or IP.</param>
'    ''' <param name="port">STUN server port. Default port is 3478.</param>
'    ''' <param name="socket">UDP socket to use.</param>
'    ''' <returns>Returns UDP netwrok info.</returns>
'    ''' <exception cref="Exception">Throws exception if unexpected error happens.</exception>
'    Public Shared Function Query(host As String, port As Integer, socket As Socket) As STUN_Result
'        If host Is Nothing Then
'            Throw New ArgumentNullException("host")
'        End If
'        If socket Is Nothing Then
'            Throw New ArgumentNullException("socket")
'        End If
'        If port < 1 Then
'            Throw New ArgumentException("Port value must be >= 1 !")
'        End If
'        If socket.ProtocolType <> ProtocolType.Udp Then
'            Throw New ArgumentException("Socket must be UDP socket !")
'        End If

'        Dim remoteEndPoint As New IPEndPoint(System.Net.Dns.GetHostAddresses(host)(0), port)

'        socket.ReceiveTimeout = 3000
'        socket.SendTimeout = 3000

'        '
'        '                In test I, the client sends a STUN Binding Request to a server, without any flags set in the
'        '                CHANGE-REQUEST attribute, and without the RESPONSE-ADDRESS attribute. This causes the server 
'        '                to send the response back to the address and port that the request came from.
'        '            
'        '                In test II, the client sends a Binding Request with both the "change IP" and "change port" flags
'        '                from the CHANGE-REQUEST attribute set.  
'        '              
'        '                In test III, the client sends a Binding Request with only the "change port" flag set.
'        '                          
'        '                                    +--------+
'        '                                    |  Test  |
'        '                                    |   I    |
'        '                                    +--------+
'        '                                         |
'        '                                         |
'        '                                         V
'        '                                        /\              /\
'        '                                     N /  \ Y          /  \ Y             +--------+
'        '                      UDP     <-------/Resp\--------->/ IP \------------->|  Test  |
'        '                      Blocked         \ ?  /          \Same/              |   II   |
'        '                                       \  /            \? /               +--------+
'        '                                        \/              \/                    |
'        '                                                         | N                  |
'        '                                                         |                    V
'        '                                                         V                    /\
'        '                                                     +--------+  Sym.      N /  \
'        '                                                     |  Test  |  UDP    <---/Resp\
'        '                                                     |   II   |  Firewall   \ ?  /
'        '                                                     +--------+              \  /
'        '                                                         |                    \/
'        '                                                         V                     |Y
'        '                              /\                         /\                    |
'        '               Symmetric  N  /  \       +--------+   N  /  \                   V
'        '                  NAT  <--- / IP \<-----|  Test  |<--- /Resp\               Open
'        '                            \Same/      |   I    |     \ ?  /               Internet
'        '                             \? /       +--------+      \  /
'        '                              \/                         \/
'        '                              |                           |Y
'        '                              |                           |
'        '                              |                           V
'        '                              |                           Full
'        '                              |                           Cone
'        '                              V              /\
'        '                          +--------+        /  \ Y
'        '                          |  Test  |------>/Resp\---->Restricted
'        '                          |   III  |       \ ?  /
'        '                          +--------+        \  /
'        '                                             \/
'        '                                              |N
'        '                                              |       Port
'        '                                              +------>Restricted
'        '
'        '            


'        ' Test I
'        Dim test1 As New STUN_Message()
'        test1.Type = STUN_MessageType.BindingRequest
'        Dim test1response As STUN_Message = DoTransaction(test1, socket, remoteEndPoint)

'        ' UDP blocked.
'        If test1response Is Nothing Then
'            Return New STUN_Result(STUN_NetType.UdpBlocked, Nothing)
'        Else
'            ' Test II
'            Dim test2 As New STUN_Message()
'            test2.Type = STUN_MessageType.BindingRequest
'            test2.ChangeRequest = New STUN_t_ChangeRequest(True, True)

'            ' No NAT.
'            If socket.LocalEndPoint.Equals(test1response.MappedAddress) Then
'                Dim test2Response As STUN_Message = DoTransaction(test2, socket, remoteEndPoint)
'                ' Open Internet.
'                If test2Response IsNot Nothing Then
'                    Return New STUN_Result(STUN_NetType.OpenInternet, test1response.MappedAddress)
'                Else
'                    ' Symmetric UDP firewall.
'                    Return New STUN_Result(STUN_NetType.SymmetricUdpFirewall, test1response.MappedAddress)
'                End If
'            Else
'                ' NAT
'                Dim test2Response As STUN_Message = DoTransaction(test2, socket, remoteEndPoint)
'                ' Full cone NAT.
'                If test2Response IsNot Nothing Then
'                    Return New STUN_Result(STUN_NetType.FullCone, test1response.MappedAddress)
'                Else
'                    '
'                    '                            If no response is received, it performs test I again, but this time, does so to 
'                    '                            the address and port from the CHANGED-ADDRESS attribute from the response to test I.
'                    '                        


'                    ' Test I(II)
'                    Dim test12 As New STUN_Message()
'                    test12.Type = STUN_MessageType.BindingRequest

'                    Dim test12Response As STUN_Message = DoTransaction(test12, socket, test1response.ChangedAddress)
'                    If test12Response Is Nothing Then
'                        Throw New Exception("STUN Test I(II) dind't get resonse !")
'                    Else
'                        ' Symmetric NAT
'                        If Not test12Response.MappedAddress.Equals(test1response.MappedAddress) Then
'                            Return New STUN_Result(STUN_NetType.Symmetric, test1response.MappedAddress)
'                        Else
'                            ' Test III
'                            Dim test3 As New STUN_Message()
'                            test3.Type = STUN_MessageType.BindingRequest
'                            test3.ChangeRequest = New STUN_t_ChangeRequest(False, True)

'                            Dim test3Response As STUN_Message = DoTransaction(test3, socket, test1response.ChangedAddress)
'                            ' Restricted
'                            If test3Response IsNot Nothing Then
'                                Return New STUN_Result(STUN_NetType.RestrictedCone, test1response.MappedAddress)
'                            Else
'                                ' Port restricted
'                                Return New STUN_Result(STUN_NetType.PortRestrictedCone, test1response.MappedAddress)
'                            End If
'                        End If
'                    End If
'                End If
'            End If
'        End If
'    End Function

'#End Region


'#Region "method DoTransaction"

'    ''' <summary>
'    ''' Does STUN transaction. Returns transaction response or null if transaction failed.
'    ''' </summary>
'    ''' <param name="request">STUN message.</param>
'    ''' <param name="socket">Socket to use for send/receive.</param>
'    ''' <param name="remoteEndPoint">Remote end point.</param>
'    ''' <returns>Returns transaction response or null if transaction failed.</returns>
'    Private Shared Function DoTransaction(request As STUN_Message, socket As Socket, remoteEndPoint As IPEndPoint) As STUN_Message
'        Dim requestBytes As Byte() = request.ToByteData()
'        Dim startTime As DateTime = DateTime.Now
'        ' We do it only 2 sec and retransmit with 100 ms.
'        While startTime.AddSeconds(2) > DateTime.Now
'            Try
'                socket.SendTo(requestBytes, remoteEndPoint)

'                ' We got response.
'                If socket.Poll(100, SelectMode.SelectRead) Then
'                    Dim receiveBuffer As Byte() = New Byte(511) {}
'                    socket.Receive(receiveBuffer)

'                    ' Parse message
'                    Dim response As New STUN_Message()
'                    response.Parse(receiveBuffer)

'                    ' Check that transaction ID matches or not response what we want.
'                    If request.TransactionID.Equals(response.TransactionID) Then
'                        Return response
'                    End If
'                End If
'            Catch
'            End Try
'        End While

'        Return Nothing
'    End Function

'#End Region





'    ''' <summary>
'    ''' Specifies UDP network type.
'    ''' </summary>
'    Public Enum STUN_NetType
'        ''' <summary>
'        ''' UDP is always blocked.
'        ''' </summary>
'        UdpBlocked

'        ''' <summary>
'        ''' No NAT, public IP, no firewall.
'        ''' </summary>
'        OpenInternet

'        ''' <summary>
'        ''' No NAT, public IP, but symmetric UDP firewall.
'        ''' </summary>
'        SymmetricUdpFirewall

'        ''' <summary>
'        ''' A full cone NAT is one where all requests from the same internal IP address and port are 
'        ''' mapped to the same external IP address and port. Furthermore, any external host can send 
'        ''' a packet to the internal host, by sending a packet to the mapped external address.
'        ''' </summary>
'        FullCone

'        ''' <summary>
'        ''' A restricted cone NAT is one where all requests from the same internal IP address and 
'        ''' port are mapped to the same external IP address and port. Unlike a full cone NAT, an external
'        ''' host (with IP address X) can send a packet to the internal host only if the internal host 
'        ''' had previously sent a packet to IP address X.
'        ''' </summary>
'        RestrictedCone

'        ''' <summary>
'        ''' A port restricted cone NAT is like a restricted cone NAT, but the restriction 
'        ''' includes port numbers. Specifically, an external host can send a packet, with source IP
'        ''' address X and source port P, to the internal host only if the internal host had previously 
'        ''' sent a packet to IP address X and port P.
'        ''' </summary>
'        PortRestrictedCone

'        ''' <summary>
'        ''' A symmetric NAT is one where all requests from the same internal IP address and port, 
'        ''' to a specific destination IP address and port, are mapped to the same external IP address and
'        ''' port.  If the same host sends a packet with the same source address and port, but to 
'        ''' a different destination, a different mapping is used. Furthermore, only the external host that
'        ''' receives a packet can send a UDP packet back to the internal host.
'        ''' </summary>
'        Symmetric
'    End Enum







'    Public Class STUN_Result
'        Private m_NetType As STUN_NetType = STUN_NetType.OpenInternet
'        Private m_pPublicEndPoint As IPEndPoint = Nothing

'        ''' <summary>
'        ''' Default constructor.
'        ''' </summary>
'        ''' <param name="netType">Specifies UDP network type.</param>
'        ''' <param name="publicEndPoint">Public IP end point.</param>
'        Public Sub New(netType As STUN_NetType, publicEndPoint As IPEndPoint)
'            m_NetType = netType
'            m_pPublicEndPoint = publicEndPoint
'        End Sub


'#Region "Properties Implementation"

'        ''' <summary>
'        ''' Gets UDP network type.
'        ''' </summary>
'        Public ReadOnly Property NetType() As STUN_NetType
'            Get
'                Return m_NetType
'            End Get
'        End Property

'        ''' <summary>
'        ''' Gets public IP end point. This value is null if failed to get network type.
'        ''' </summary>
'        Public ReadOnly Property PublicEndPoint() As IPEndPoint
'            Get
'                Return m_pPublicEndPoint
'            End Get
'        End Property

'#End Region

'    End Class









'    Public Class STUN_t_ErrorCode
'        Private m_Code As Integer = 0
'        Private m_ReasonText As String = ""

'        ''' <summary>
'        ''' Default constructor.
'        ''' </summary>
'        ''' <param name="code">Error code.</param>
'        ''' <param name="reasonText">Reason text.</param>
'        Public Sub New(code As Integer, reasonText As String)
'            m_Code = code
'            m_ReasonText = reasonText
'        End Sub


'#Region "Properties Implementation"

'        ''' <summary>
'        ''' Gets or sets error code.
'        ''' </summary>
'        Public Property Code() As Integer
'            Get
'                Return m_Code
'            End Get

'            Set(value As Integer)
'                m_Code = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets reason text.
'        ''' </summary>
'        Public Property ReasonText() As String
'            Get
'                Return m_ReasonText
'            End Get

'            Set(value As String)
'                m_ReasonText = value
'            End Set
'        End Property

'#End Region

'    End Class


'    ''' <summary>
'    ''' This class implements STUN CHANGE-REQUEST attribute. Defined in RFC 3489 11.2.4.
'    ''' </summary>
'    Public Class STUN_t_ChangeRequest
'        Private m_ChangeIP As Boolean = True
'        Private m_ChangePort As Boolean = True

'        ''' <summary>
'        ''' Default constructor.
'        ''' </summary>
'        Public Sub New()
'        End Sub

'        ''' <summary>
'        ''' Default constructor.
'        ''' </summary>
'        ''' <param name="changeIP">Specifies if STUN server must send response to different IP than request was received.</param>
'        ''' <param name="changePort">Specifies if STUN server must send response to different port than request was received.</param>
'        Public Sub New(changeIP As Boolean, changePort As Boolean)
'            m_ChangeIP = changeIP
'            m_ChangePort = changePort
'        End Sub


'#Region "Properties Implementation"

'        ''' <summary>
'        ''' Gets or sets if STUN server must send response to different IP than request was received.
'        ''' </summary>
'        Public Property ChangeIP() As Boolean
'            Get
'                Return m_ChangeIP
'            End Get

'            Set(value As Boolean)
'                m_ChangeIP = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets or sets if STUN server must send response to different port than request was received.
'        ''' </summary>
'        Public Property ChangePort() As Boolean
'            Get
'                Return m_ChangePort
'            End Get

'            Set(value As Boolean)
'                m_ChangePort = value
'            End Set
'        End Property

'#End Region

'    End Class




'    ''' <summary>
'    ''' This enum specifies STUN message type.
'    ''' </summary>
'    Public Enum STUN_MessageType
'        ''' <summary>
'        ''' STUN message is binding request.
'        ''' </summary>
'        BindingRequest = &H1

'        ''' <summary>
'        ''' STUN message is binding request response.
'        ''' </summary>
'        BindingResponse = &H101

'        ''' <summary>
'        ''' STUN message is binding requesr error response.
'        ''' </summary>
'        BindingErrorResponse = &H111

'        ''' <summary>
'        ''' STUN message is "shared secret" request.
'        ''' </summary>
'        SharedSecretRequest = &H2

'        ''' <summary>
'        ''' STUN message is "shared secret" request response.
'        ''' </summary>
'        SharedSecretResponse = &H102

'        ''' <summary>
'        ''' STUN message is "shared secret" request error response.
'        ''' </summary>
'        SharedSecretErrorResponse = &H112
'    End Enum





'    ''' <summary>
'    ''' Implements STUN message. Defined in RFC 3489.
'    ''' </summary>
'    Public Class STUN_Message
'#Region "enum AttributeType"

'        ''' <summary>
'        ''' Specifies STUN attribute type.
'        ''' </summary>
'        Private Enum AttributeType
'            MappedAddress = &H1
'            ResponseAddress = &H2
'            ChangeRequest = &H3
'            SourceAddress = &H4
'            ChangedAddress = &H5
'            Username = &H6
'            Password = &H7
'            MessageIntegrity = &H8
'            ErrorCode = &H9
'            UnknownAttribute = &HA
'            ReflectedFrom = &HB
'            XorMappedAddress = &H8020
'            XorOnly = &H21
'            ServerName = &H8022
'        End Enum

'#End Region

'#Region "enum IPFamily"

'        ''' <summary>
'        ''' Specifies IP address family.
'        ''' </summary>
'        Private Enum IPFamily
'            IPv4 = &H1
'            IPv6 = &H2
'        End Enum

'#End Region

'        Private m_Type As STUN_MessageType = STUN_MessageType.BindingRequest
'        Private m_pTransactionID As Guid = Guid.Empty
'        Private m_pMappedAddress As IPEndPoint = Nothing
'        Private m_pResponseAddress As IPEndPoint = Nothing
'        Private m_pChangeRequest As STUN_t_ChangeRequest = Nothing
'        Private m_pSourceAddress As IPEndPoint = Nothing
'        Private m_pChangedAddress As IPEndPoint = Nothing
'        Private m_UserName As String = Nothing
'        Private m_Password As String = Nothing
'        Private m_pErrorCode As STUN_t_ErrorCode = Nothing
'        Private m_pReflectedFrom As IPEndPoint = Nothing
'        Private m_ServerName As String = Nothing

'        ''' <summary>
'        ''' Default constructor.
'        ''' </summary>
'        Public Sub New()
'            m_pTransactionID = Guid.NewGuid()
'        End Sub


'#Region "method Parse"

'        ''' <summary>
'        ''' Parses STUN message from raw data packet.
'        ''' </summary>
'        ''' <param name="data">Raw STUN message.</param>
'        Public Sub Parse(data As Byte())
'            ' RFC 3489 11.1.             
'            '                All STUN messages consist of a 20 byte header:
'            '
'            '                0                   1                   2                   3
'            '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '               |      STUN Message Type        |         Message Length        |
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '               |
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '                                        Transaction ID
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '                                                                               |
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '              
'            '               The message length is the count, in bytes, of the size of the
'            '               message, not including the 20 byte header.
'            '            


'            If data.Length < 20 Then
'                Throw New ArgumentException("Invalid STUN message value !")
'            End If

'            Dim offset As Integer = 0

'            '--- message header --------------------------------------------------

'            ' STUN Message Type
'            Dim messageType As Integer = (data(offset + 1) << 8) Or data(offset + 1)
'            If messageType = CInt(STUN_MessageType.BindingErrorResponse) Then
'                m_Type = STUN_MessageType.BindingErrorResponse
'            ElseIf messageType = CInt(STUN_MessageType.BindingRequest) Then
'                m_Type = STUN_MessageType.BindingRequest
'            ElseIf messageType = CInt(STUN_MessageType.BindingResponse) Then
'                m_Type = STUN_MessageType.BindingResponse
'            ElseIf messageType = CInt(STUN_MessageType.SharedSecretErrorResponse) Then
'                m_Type = STUN_MessageType.SharedSecretErrorResponse
'            ElseIf messageType = CInt(STUN_MessageType.SharedSecretRequest) Then
'                m_Type = STUN_MessageType.SharedSecretRequest
'            ElseIf messageType = CInt(STUN_MessageType.SharedSecretResponse) Then
'                m_Type = STUN_MessageType.SharedSecretResponse
'            Else
'                Throw New ArgumentException("Invalid STUN message type value !")
'            End If

'            ' Message Length
'            Dim messageLength As Integer = (data(offset + 1) << 8) Or data(offset + 1)

'            ' Transaction ID
'            Dim guid As Byte() = New Byte(15) {}
'            Array.Copy(data, offset, guid, 0, 16)
'            m_pTransactionID = New Guid(guid)
'            offset += 16

'            '--- Message attributes ---------------------------------------------
'            While (offset - 20) < messageLength
'                ParseAttribute(data, offset)
'            End While
'        End Sub

'#End Region

'#Region "method ToByteData"

'        ''' <summary>
'        ''' Converts this to raw STUN packet.
'        ''' </summary>
'        ''' <returns>Returns raw STUN packet.</returns>
'        Public Function ToByteData() As Byte()
'            ' RFC 3489 11.1.             
'            '                All STUN messages consist of a 20 byte header:
'            '
'            '                0                   1                   2                   3
'            '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '               |      STUN Message Type        |         Message Length        |
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '               |
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '                                        Transaction ID
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '                                                                               |
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '             
'            '               The message length is the count, in bytes, of the size of the
'            '               message, not including the 20 byte header.
'            '
'            '            


'            ' We allocate 512 for header, that should be more than enough.
'            Dim msg As Byte() = New Byte(511) {}

'            Dim offset As Integer = 0

'            '--- message header -------------------------------------

'            ' STUN Message Type (2 bytes)
'            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(CInt(Me.Type) >> 8)
'            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(CInt(Me.Type) And &HFF)

'            ' Message Length (2 bytes) will be assigned at last.
'            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0

'            ' Transaction ID (16 bytes)
'            Array.Copy(m_pTransactionID.ToByteArray(), 0, msg, offset, 16)
'            offset += 16

'            '--- Message attributes ------------------------------------

'            ' RFC 3489 11.2.
'            '                After the header are 0 or more attributes.  Each attribute is TLV
'            '                encoded, with a 16 bit type, 16 bit length, and variable value:
'            '
'            '                0                   1                   2                   3
'            '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '               |         Type                  |            Length             |
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '               |                             Value                             ....
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '            


'            If Me.MappedAddress IsNot Nothing Then
'                StoreEndPoint(AttributeType.MappedAddress, Me.MappedAddress, msg, offset)
'            ElseIf Me.ResponseAddress IsNot Nothing Then
'                StoreEndPoint(AttributeType.ResponseAddress, Me.ResponseAddress, msg, offset)
'            ElseIf Me.ChangeRequest IsNot Nothing Then
'                '
'                '                    The CHANGE-REQUEST attribute is used by the client to request that
'                '                    the server use a different address and/or port when sending the
'                '                    response.  The attribute is 32 bits long, although only two bits (A
'                '                    and B) are used:
'                '
'                '                     0                   1                   2                   3
'                '                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '                    |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '
'                '                    The meaning of the flags is:
'                '
'                '                    A: This is the "change IP" flag.  If true, it requests the server
'                '                       to send the Binding Response with a different IP address than the
'                '                       one the Binding Request was received on.
'                '
'                '                    B: This is the "change port" flag.  If true, it requests the
'                '                       server to send the Binding Response with a different port than the
'                '                       one the Binding Request was received on.
'                '                


'                ' Attribute header
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.ChangeRequest) >> 8
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.ChangeRequest) And &HFF
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 4

'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(Convert.ToInt32(Me.ChangeRequest.ChangeIP) << 2 Or Convert.ToInt32(Me.ChangeRequest.ChangePort) << 1)
'            ElseIf Me.SourceAddress IsNot Nothing Then
'                StoreEndPoint(AttributeType.SourceAddress, Me.SourceAddress, msg, offset)
'            ElseIf Me.ChangedAddress IsNot Nothing Then
'                StoreEndPoint(AttributeType.ChangedAddress, Me.ChangedAddress, msg, offset)
'            ElseIf Me.UserName IsNot Nothing Then
'                Dim userBytes As Byte() = Encoding.ASCII.GetBytes(Me.UserName)

'                ' Attribute header
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.Username) >> 8
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.Username) And &HFF
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(userBytes.Length >> 8)
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(userBytes.Length And &HFF)

'                Array.Copy(userBytes, 0, msg, offset, userBytes.Length)
'                offset += userBytes.Length
'            ElseIf Me.Password IsNot Nothing Then
'                Dim userBytes As Byte() = Encoding.ASCII.GetBytes(Me.UserName)

'                ' Attribute header
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.Password) >> 8
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.Password) And &HFF
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(userBytes.Length >> 8)
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(userBytes.Length And &HFF)

'                Array.Copy(userBytes, 0, msg, offset, userBytes.Length)
'                offset += userBytes.Length
'            ElseIf Me.ErrorCode IsNot Nothing Then
'                ' 3489 11.2.9.
'                '                    0                   1                   2                   3
'                '                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '                    |                   0                     |Class|     Number    |
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '                    |      Reason Phrase (variable)                                ..
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '                


'                Dim reasonBytes As Byte() = Encoding.ASCII.GetBytes(Me.ErrorCode.ReasonText)

'                ' Header
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.ErrorCode)
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(4 + reasonBytes.Length)

'                ' Empty
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'                ' Class
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(Math.Floor(CDbl(Me.ErrorCode.Code / 100)))
'                ' Number
'                msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(Me.ErrorCode.Code And &HFF)
'                ' ReasonPhrase
'                Array.Copy(reasonBytes, msg, reasonBytes.Length)
'                offset += reasonBytes.Length
'            ElseIf Me.ReflectedFrom IsNot Nothing Then
'                StoreEndPoint(AttributeType.ReflectedFrom, Me.ReflectedFrom, msg, offset)
'            End If

'            ' Update Message Length. NOTE: 20 bytes header not included.
'            msg(2) = CByte((offset - 20) >> 8)
'            msg(3) = CByte((offset - 20) And &HFF)

'            ' Make reatval with actual size.
'            Dim retVal As Byte() = New Byte(offset - 1) {}
'            Array.Copy(msg, retVal, retVal.Length)

'            Return retVal
'        End Function

'#End Region


'#Region "method ParseAttribute"

'        ''' <summary>
'        ''' Parses attribute from data.
'        ''' </summary>
'        ''' <param name="data">SIP message data.</param>
'        ''' <param name="offset">Offset in data.</param>
'        Private Sub ParseAttribute(data As Byte(), ByRef offset As Integer)
'            ' RFC 3489 11.2.
'            '                Each attribute is TLV encoded, with a 16 bit type, 16 bit length, and variable value:
'            '
'            '                0                   1                   2                   3
'            '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '               |         Type                  |            Length             |
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '               |                             Value                             ....
'            '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                            
'            '            


'            ' Type
'            Dim type As AttributeType = CType(data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 8 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)), AttributeType)

'            ' Length
'            Dim length As Integer = (data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 8 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)))

'            ' MAPPED-ADDRESS
'            If type = AttributeType.MappedAddress Then
'                m_pMappedAddress = ParseEndPoint(data, offset)
'                ' RESPONSE-ADDRESS
'            ElseIf type = AttributeType.ResponseAddress Then
'                m_pResponseAddress = ParseEndPoint(data, offset)
'                ' CHANGE-REQUEST
'            ElseIf type = AttributeType.ChangeRequest Then
'                '
'                '                    The CHANGE-REQUEST attribute is used by the client to request that
'                '                    the server use a different address and/or port when sending the
'                '                    response.  The attribute is 32 bits long, although only two bits (A
'                '                    and B) are used:
'                '
'                '                     0                   1                   2                   3
'                '                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '                    |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '
'                '                    The meaning of the flags is:
'                '
'                '                    A: This is the "change IP" flag.  If true, it requests the server
'                '                       to send the Binding Response with a different IP address than the
'                '                       one the Binding Request was received on.
'                '
'                '                    B: This is the "change port" flag.  If true, it requests the
'                '                       server to send the Binding Response with a different port than the
'                '                       one the Binding Request was received on.
'                '                


'                ' Skip 3 bytes
'                offset += 3

'                m_pChangeRequest = New STUN_t_ChangeRequest((data(offset) And 4) <> 0, (data(offset) And 2) <> 0)
'                offset += 1
'                ' SOURCE-ADDRESS
'            ElseIf type = AttributeType.SourceAddress Then
'                m_pSourceAddress = ParseEndPoint(data, offset)
'                ' CHANGED-ADDRESS
'            ElseIf type = AttributeType.ChangedAddress Then
'                m_pChangedAddress = ParseEndPoint(data, offset)
'                ' USERNAME
'            ElseIf type = AttributeType.Username Then
'                m_UserName = Encoding.[Default].GetString(data, offset, length)
'                offset += length
'                ' PASSWORD
'            ElseIf type = AttributeType.Password Then
'                m_Password = Encoding.[Default].GetString(data, offset, length)
'                offset += length
'                ' MESSAGE-INTEGRITY
'            ElseIf type = AttributeType.MessageIntegrity Then
'                offset += length
'                ' ERROR-CODE
'            ElseIf type = AttributeType.ErrorCode Then
'                ' 3489 11.2.9.
'                '                    0                   1                   2                   3
'                '                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '                    |                   0                     |Class|     Number    |
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '                    |      Reason Phrase (variable)                                ..
'                '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'                '                


'                Dim errorCode As Integer = (data(offset + 2) And &H7) * 100 + (data(offset + 3) And &HFF)

'                m_pErrorCode = New STUN_t_ErrorCode(errorCode, Encoding.[Default].GetString(data, offset + 4, length - 4))
'                offset += length
'                ' UNKNOWN-ATTRIBUTES
'            ElseIf type = AttributeType.UnknownAttribute Then
'                offset += length
'                ' REFLECTED-FROM
'            ElseIf type = AttributeType.ReflectedFrom Then
'                m_pReflectedFrom = ParseEndPoint(data, offset)
'                ' XorMappedAddress
'                ' XorOnly
'                ' ServerName
'            ElseIf type = AttributeType.ServerName Then
'                m_ServerName = Encoding.[Default].GetString(data, offset, length)
'                offset += length
'            Else
'                ' Unknown
'                offset += length
'            End If
'        End Sub

'#End Region

'#Region "method ParseEndPoint"

'        ''' <summary>
'        ''' Pasrses IP endpoint attribute.
'        ''' </summary>
'        ''' <param name="data">STUN message data.</param>
'        ''' <param name="offset">Offset in data.</param>
'        ''' <returns>Returns parsed IP end point.</returns>
'        Private Function ParseEndPoint(data As Byte(), ByRef offset As Integer) As IPEndPoint
'            '
'            '                It consists of an eight bit address family, and a sixteen bit
'            '                port, followed by a fixed length value representing the IP address.
'            '
'            '                0                   1                   2                   3
'            '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'            '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '                |x x x x x x x x|    Family     |           Port                |
'            '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '                |                             Address                           |
'            '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '            


'            ' Skip family
'            offset += 1
'            offset += 1

'            ' Port
'            Dim port As Integer = (data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 8 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)))

'            ' Address
'            Dim ip As Byte() = New Byte(3) {}
'            ip(0) = data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1))
'            ip(1) = data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1))
'            ip(2) = data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1))
'            ip(3) = data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1))

'            Return New IPEndPoint(New IPAddress(ip), port)
'        End Function

'#End Region

'#Region "method StoreEndPoint"

'        ''' <summary>
'        ''' Stores ip end point attribute to buffer.
'        ''' </summary>
'        ''' <param name="type">Attribute type.</param>
'        ''' <param name="endPoint">IP end point.</param>
'        ''' <param name="message">Buffer where to store.</param>
'        ''' <param name="offset">Offset in buffer.</param>
'        Private Sub StoreEndPoint(type As AttributeType, endPoint As IPEndPoint, message As Byte(), ByRef offset As Integer)
'            '
'            '                It consists of an eight bit address family, and a sixteen bit
'            '                port, followed by a fixed length value representing the IP address.
'            '
'            '                0                   1                   2                   3
'            '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
'            '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '                |x x x x x x x x|    Family     |           Port                |
'            '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
'            '                |                             Address                           |
'            '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+             
'            '            


'            ' Header
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(CInt(type) >> 8)
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(CInt(type) And &HFF)
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 8

'            ' Unused
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
'            ' Family
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(IPFamily.IPv4)
'            ' Port
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(endPoint.Port >> 8)
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(endPoint.Port And &HFF)
'            ' Address
'            Dim ipBytes As Byte() = endPoint.Address.GetAddressBytes()
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = ipBytes(0)
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = ipBytes(0)
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = ipBytes(0)
'            message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = ipBytes(0)
'        End Sub

'#End Region


'#Region "Properties Implementation"

'        ''' <summary>
'        ''' Gets STUN message type.
'        ''' </summary>
'        Public Property Type() As STUN_MessageType
'            Get
'                Return m_Type
'            End Get

'            Set(value As STUN_MessageType)
'                m_Type = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets transaction ID.
'        ''' </summary>
'        Public ReadOnly Property TransactionID() As Guid
'            Get
'                Return m_pTransactionID
'            End Get
'        End Property

'        ''' <summary>
'        ''' Gets or sets IP end point what was actually connected to STUN server. Returns null if not specified.
'        ''' </summary>
'        Public Property MappedAddress() As IPEndPoint
'            Get
'                Return m_pMappedAddress
'            End Get

'            Set(value As IPEndPoint)
'                m_pMappedAddress = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets or sets IP end point where to STUN client likes to receive response.
'        ''' Value null means not specified.
'        ''' </summary>
'        Public Property ResponseAddress() As IPEndPoint
'            Get
'                Return m_pResponseAddress
'            End Get

'            Set(value As IPEndPoint)
'                m_pResponseAddress = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets or sets how and where STUN server must send response back to STUN client.
'        ''' Value null means not specified.
'        ''' </summary>
'        Public Property ChangeRequest() As STUN_t_ChangeRequest
'            Get
'                Return m_pChangeRequest
'            End Get

'            Set(value As STUN_t_ChangeRequest)
'                m_pChangeRequest = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets or sets STUN server IP end point what sent response to STUN client. Value null
'        ''' means not specified.
'        ''' </summary>
'        Public Property SourceAddress() As IPEndPoint
'            Get
'                Return m_pSourceAddress
'            End Get

'            Set(value As IPEndPoint)
'                m_pSourceAddress = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets or sets IP end point where STUN server will send response back to STUN client 
'        ''' if the "change IP" and "change port" flags had been set in the ChangeRequest.
'        ''' </summary>
'        Public Property ChangedAddress() As IPEndPoint
'            Get
'                Return m_pChangedAddress
'            End Get

'            Set(value As IPEndPoint)
'                m_pChangedAddress = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets or sets user name. Value null means not specified.
'        ''' </summary>          
'        Public Property UserName() As String
'            Get
'                Return m_UserName
'            End Get

'            Set(value As String)
'                m_UserName = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets or sets password. Value null means not specified.
'        ''' </summary>
'        Public Property Password() As String
'            Get
'                Return m_Password
'            End Get

'            Set(value As String)
'                m_Password = value
'            End Set
'        End Property

'        'public MessageIntegrity

'        ''' <summary>
'        ''' Gets or sets error info. Returns null if not specified.
'        ''' </summary>
'        Public Property ErrorCode() As STUN_t_ErrorCode
'            Get
'                Return m_pErrorCode
'            End Get

'            Set(value As STUN_t_ErrorCode)
'                m_pErrorCode = value
'            End Set
'        End Property


'        ''' <summary>
'        ''' Gets or sets IP endpoint from which IP end point STUN server got STUN client request.
'        ''' Value null means not specified.
'        ''' </summary>
'        Public Property ReflectedFrom() As IPEndPoint
'            Get
'                Return m_pReflectedFrom
'            End Get

'            Set(value As IPEndPoint)
'                m_pReflectedFrom = value
'            End Set
'        End Property

'        ''' <summary>
'        ''' Gets or sets server name.
'        ''' </summary>
'        Public Property ServerName() As String
'            Get
'                Return m_ServerName
'            End Get

'            Set(value As String)
'                m_ServerName = value
'            End Set
'        End Property

'#End Region

'    End Class



'End Class



Public Class STUN_Result
    Private m_NetType As STUN_NetType = STUN_NetType.OpenInternet
    Private m_pPublicEndPoint As IPEndPoint = Nothing

    ''' <summary>
    ''' Default constructor.
    ''' </summary>
    ''' <param name="netType">Specifies UDP network type.</param>
    ''' <param name="publicEndPoint">Public IP end point.</param>
    Public Sub New(netType As STUN_NetType, publicEndPoint As IPEndPoint)
        m_NetType = netType
        m_pPublicEndPoint = publicEndPoint
    End Sub


#Region "Properties Implementation"

    ''' <summary>
    ''' Gets UDP network type.
    ''' </summary>
    Public ReadOnly Property NetType() As STUN_NetType
        Get
            Return m_NetType
        End Get
    End Property

    ''' <summary>
    ''' Gets public IP end point. This value is null if failed to get network type.
    ''' </summary>
    Public ReadOnly Property PublicEndPoint() As IPEndPoint
        Get
            Return m_pPublicEndPoint
        End Get
    End Property

#End Region

End Class


''' <summary>
''' Specifies UDP network type.
''' </summary>
Public Enum STUN_NetType
    ''' <summary>
    ''' UDP is always blocked.
    ''' </summary>
    UdpBlocked

    ''' <summary>
    ''' No NAT, public IP, no firewall.
    ''' </summary>
    OpenInternet

    ''' <summary>
    ''' No NAT, public IP, but symmetric UDP firewall.
    ''' </summary>
    SymmetricUdpFirewall

    ''' <summary>
    ''' A full cone NAT is one where all requests from the same internal IP address and port are 
    ''' mapped to the same external IP address and port. Furthermore, any external host can send 
    ''' a packet to the internal host, by sending a packet to the mapped external address.
    ''' </summary>
    FullCone

    ''' <summary>
    ''' A restricted cone NAT is one where all requests from the same internal IP address and 
    ''' port are mapped to the same external IP address and port. Unlike a full cone NAT, an external
    ''' host (with IP address X) can send a packet to the internal host only if the internal host 
    ''' had previously sent a packet to IP address X.
    ''' </summary>
    RestrictedCone

    ''' <summary>
    ''' A port restricted cone NAT is like a restricted cone NAT, but the restriction 
    ''' includes port numbers. Specifically, an external host can send a packet, with source IP
    ''' address X and source port P, to the internal host only if the internal host had previously 
    ''' sent a packet to IP address X and port P.
    ''' </summary>
    PortRestrictedCone

    ''' <summary>
    ''' A symmetric NAT is one where all requests from the same internal IP address and port, 
    ''' to a specific destination IP address and port, are mapped to the same external IP address and
    ''' port.  If the same host sends a packet with the same source address and port, but to 
    ''' a different destination, a different mapping is used. Furthermore, only the external host that
    ''' receives a packet can send a UDP packet back to the internal host.
    ''' </summary>
    Symmetric
End Enum


''' <summary>
''' This class implements STUN client. Defined in RFC 3489.
''' </summary>
''' <example>
''' <code>
''' // Create new socket for STUN client.
''' Socket socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
''' socket.Bind(new IPEndPoint(IPAddress.Any,0));
''' 
''' // Query STUN server
''' STUN_Result result = STUN_Client.Query("stunserver.org",3478,socket);
''' if(result.NetType != STUN_NetType.UdpBlocked){
'''     // UDP blocked or !!!! bad STUN server
''' }
''' else{
'''     IPEndPoint publicEP = result.PublicEndPoint;
'''     // Do your stuff
''' }
''' </code>
''' </example>
Public Class STUN_Client
#Region "static method Query"

    ''' <summary>
    ''' Gets NAT info from STUN server.
    ''' </summary>
    ''' <param name="host">STUN server name or IP.</param>
    ''' <param name="port">STUN server port. Default port is 3478.</param>
    ''' <param name="localEP">Local IP end point.</param>
    ''' <returns>Returns UDP netwrok info.</returns>
    ''' <exception cref="ArgumentNullException">Is raised when <b>host</b> or <b>localEP</b> is null reference.</exception>
    ''' <exception cref="Exception">Throws exception if unexpected error happens.</exception>
    Public Shared Function Query(host As String, port As Integer, localEP As IPEndPoint) As STUN_Result
        If host Is Nothing Then
            Throw New ArgumentNullException("host")
        End If
        If localEP Is Nothing Then
            Throw New ArgumentNullException("localEP")
        End If

        Using s As New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            s.Bind(localEP)

            Return Query(host, port, s)
        End Using
    End Function

    ''' <summary>
    ''' Gets NAT info from STUN server.
    ''' </summary>
    ''' <param name="host">STUN server name or IP.</param>
    ''' <param name="port">STUN server port. Default port is 3478.</param>
    ''' <param name="socket">UDP socket to use.</param>
    ''' <returns>Returns UDP netwrok info.</returns>
    ''' <exception cref="Exception">Throws exception if unexpected error happens.</exception>
    Public Shared Function Query(host As String, port As Integer, socket As Socket) As STUN_Result
        If host Is Nothing Then
            Throw New ArgumentNullException("host")
        End If
        If socket Is Nothing Then
            Throw New ArgumentNullException("socket")
        End If
        If port < 1 Then
            Throw New ArgumentException("Port value must be >= 1 !")
        End If
        If socket.ProtocolType <> ProtocolType.Udp Then
            Throw New ArgumentException("Socket must be UDP socket !")
        End If

        Dim remoteEndPoint As New IPEndPoint(System.Net.Dns.GetHostAddresses(host)(0), port)

        '
        '                In test I, the client sends a STUN Binding Request to a server, without any flags set in the
        '                CHANGE-REQUEST attribute, and without the RESPONSE-ADDRESS attribute. This causes the server 
        '                to send the response back to the address and port that the request came from.
        '            
        '                In test II, the client sends a Binding Request with both the "change IP" and "change port" flags
        '                from the CHANGE-REQUEST attribute set.  
        '              
        '                In test III, the client sends a Binding Request with only the "change port" flag set.
        '                          
        '                                    +--------+
        '                                    |  Test  |
        '                                    |   I    |
        '                                    +--------+
        '                                         |
        '                                         |
        '                                         V
        '                                        /\              /\
        '                                     N /  \ Y          /  \ Y             +--------+
        '                      UDP     <-------/Resp\--------->/ IP \------------->|  Test  |
        '                      Blocked         \ ?  /          \Same/              |   II   |
        '                                       \  /            \? /               +--------+
        '                                        \/              \/                    |
        '                                                         | N                  |
        '                                                         |                    V
        '                                                         V                    /\
        '                                                     +--------+  Sym.      N /  \
        '                                                     |  Test  |  UDP    <---/Resp\
        '                                                     |   II   |  Firewall   \ ?  /
        '                                                     +--------+              \  /
        '                                                         |                    \/
        '                                                         V                     |Y
        '                              /\                         /\                    |
        '               Symmetric  N  /  \       +--------+   N  /  \                   V
        '                  NAT  <--- / IP \<-----|  Test  |<--- /Resp\               Open
        '                            \Same/      |   I    |     \ ?  /               Internet
        '                             \? /       +--------+      \  /
        '                              \/                         \/
        '                              |                           |Y
        '                              |                           |
        '                              |                           V
        '                              |                           Full
        '                              |                           Cone
        '                              V              /\
        '                          +--------+        /  \ Y
        '                          |  Test  |------>/Resp\---->Restricted
        '                          |   III  |       \ ?  /
        '                          +--------+        \  /
        '                                             \/
        '                                              |N
        '                                              |       Port
        '                                              +------>Restricted
        '
        '            


        Try
            ' Test I
            Dim test1 As New STUN_Message()
            test1.Type = STUN_MessageType.BindingRequest
            Dim test1response As STUN_Message = DoTransaction(test1, socket, remoteEndPoint, 1600)

            ' UDP blocked.
            If test1response Is Nothing Then
                Return New STUN_Result(STUN_NetType.UdpBlocked, Nothing)
            Else
                ' Test II
                Dim test2 As New STUN_Message()
                test2.Type = STUN_MessageType.BindingRequest
                test2.ChangeRequest = New STUN_t_ChangeRequest(True, True)

                ' No NAT.
                If socket.LocalEndPoint.Equals(test1response.MappedAddress) Then
                    Dim test2Response As STUN_Message = DoTransaction(test2, socket, remoteEndPoint, 1600)
                    ' Open Internet.
                    If test2Response IsNot Nothing Then
                        Return New STUN_Result(STUN_NetType.OpenInternet, test1response.MappedAddress)
                    Else
                        ' Symmetric UDP firewall.
                        Return New STUN_Result(STUN_NetType.SymmetricUdpFirewall, test1response.MappedAddress)
                    End If
                Else
                    ' NAT
                    Dim test2Response As STUN_Message = DoTransaction(test2, socket, remoteEndPoint, 1600)

                    ' Full cone NAT.
                    If test2Response IsNot Nothing Then
                        Return New STUN_Result(STUN_NetType.FullCone, test1response.MappedAddress)
                    Else
                        '
                        '                                If no response is received, it performs test I again, but this time, does so to 
                        '                                the address and port from the CHANGED-ADDRESS attribute from the response to test I.
                        '                            


                        ' Test I(II)
                        Dim test12 As New STUN_Message()
                        test12.Type = STUN_MessageType.BindingRequest

                        Dim test12Response As STUN_Message = DoTransaction(test12, socket, test1response.ChangedAddress, 1600)
                        If test12Response Is Nothing Then
                            Throw New Exception("STUN Test I(II) dind't get resonse !")
                        Else
                            ' Symmetric NAT
                            If Not test12Response.MappedAddress.Equals(test1response.MappedAddress) Then
                                Return New STUN_Result(STUN_NetType.Symmetric, test1response.MappedAddress)
                            Else
                                ' Test III
                                Dim test3 As New STUN_Message()
                                test3.Type = STUN_MessageType.BindingRequest
                                test3.ChangeRequest = New STUN_t_ChangeRequest(False, True)

                                Dim test3Response As STUN_Message = DoTransaction(test3, socket, test1response.ChangedAddress, 1600)
                                ' Restricted
                                If test3Response IsNot Nothing Then
                                    Return New STUN_Result(STUN_NetType.RestrictedCone, test1response.MappedAddress)
                                Else
                                    ' Port restricted
                                    Return New STUN_Result(STUN_NetType.PortRestrictedCone, test1response.MappedAddress)
                                End If
                            End If
                        End If
                    End If
                End If
            End If
        Finally
            ' Junk all late responses.
            Dim startTime As DateTime = DateTime.Now
            While startTime.AddMilliseconds(200) > DateTime.Now
                ' We got response.
                If socket.Poll(1, SelectMode.SelectRead) Then
                    Dim receiveBuffer As Byte() = New Byte(511) {}
                    socket.Receive(receiveBuffer)
                End If
            End While
        End Try
    End Function

#End Region

#Region "method GetPublicIP"


    Public Shared Function IsPrivateIP(ip As IPAddress) As Boolean
        If ip Is Nothing Then
            Throw New ArgumentNullException("ip")
        End If

        If ip.AddressFamily = System.Net.Sockets.AddressFamily.InterNetwork Then
            Dim ipBytes As Byte() = ip.GetAddressBytes()

            ' Private IPs:
            '					First Octet = 192 AND Second Octet = 168 (Example: 192.168.X.X) 
            '					First Octet = 172 AND (Second Octet >= 16 AND Second Octet <= 31) (Example: 172.16.X.X - 172.31.X.X)
            '					First Octet = 10 (Example: 10.X.X.X)
            '					First Octet = 169 AND Second Octet = 254 (Example: 169.254.X.X)
            '
            '				


            If ipBytes(0) = 192 AndAlso ipBytes(1) = 168 Then
                Return True
            End If
            If ipBytes(0) = 172 AndAlso ipBytes(1) >= 16 AndAlso ipBytes(1) <= 31 Then
                Return True
            End If
            If ipBytes(0) = 10 Then
                Return True
            End If
            If ipBytes(0) = 169 AndAlso ipBytes(1) = 254 Then
                Return True
            End If
        End If

        Return False
    End Function






    Public Shared Function CreateSocket(localEP As IPEndPoint, protocolType__1 As ProtocolType) As Socket
        If localEP Is Nothing Then
            Throw New ArgumentNullException("localEP")
        End If

        Dim socketType__2 As SocketType = SocketType.Stream
        If protocolType__1 = ProtocolType.Udp Then
            socketType__2 = SocketType.Dgram
        End If

        If localEP.AddressFamily = AddressFamily.InterNetwork Then
            Dim socket As New Socket(AddressFamily.InterNetwork, socketType__2, protocolType__1)
            socket.Bind(localEP)

            Return socket
        ElseIf localEP.AddressFamily = AddressFamily.InterNetworkV6 Then
            Dim socket As New Socket(AddressFamily.InterNetworkV6, socketType__2, protocolType__1)
            socket.Bind(localEP)

            Return socket
        Else
            Throw New ArgumentException("Invalid IPEndPoint address family.")
        End If
    End Function






    Public Shared Function CompareArray(array1 As Array, array2 As Array) As Boolean
        Return CompareArray(array1, array2, array2.Length)
    End Function

    ''' <summary>
    ''' Compares if specified array itmes equals.
    ''' </summary>
    ''' <param name="array1">Array 1.</param>
    ''' <param name="array2">Array 2</param>
    ''' <param name="array2Count">Number of bytes in array 2 used for compare.</param>
    ''' <returns>Returns true if both arrays are equal.</returns>
    Public Shared Function CompareArray(array1 As Array, array2 As Array, array2Count As Integer) As Boolean
        If array1 Is Nothing AndAlso array2 Is Nothing Then
            Return True
        End If
        If array1 Is Nothing AndAlso array2 IsNot Nothing Then
            Return False
        End If
        If array1 IsNot Nothing AndAlso array2 Is Nothing Then
            Return False
        End If
        If array1.Length <> array2Count Then
            Return False
        Else
            For i As Integer = 0 To array1.Length - 1
                If Not array1.GetValue(i).Equals(array2.GetValue(i)) Then
                    Return False
                End If
            Next
        End If

        Return True
    End Function
    ''' <summary>
    ''' Resolves local IP to public IP using STUN.
    ''' </summary>
    ''' <param name="stunServer">STUN server.</param>
    ''' <param name="port">STUN server port. Default port is 3478.</param>
    ''' <param name="localIP">Local IP address.</param>
    ''' <returns>Returns public IP address.</returns>
    ''' <exception cref="ArgumentNullException">Is raised when <b>stunServer</b> or <b>localIP</b> is null reference.</exception>
    ''' <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
    ''' <exception cref="IOException">Is raised when no connection to STUN server.</exception>
    Public Shared Function GetPublicIP(stunServer As String, port As Integer, localIP As IPAddress) As IPAddress
        If stunServer Is Nothing Then
            Throw New ArgumentNullException("stunServer")
        End If
        If stunServer = "" Then
            Throw New ArgumentException("Argument 'stunServer' value must be specified.")
        End If
        If port < 1 Then
            Throw New ArgumentException("Invalid argument 'port' value.")
        End If
        If localIP Is Nothing Then
            Throw New ArgumentNullException("localIP")
        End If

        If Not IsPrivateIP(localIP) Then
            Return localIP
        End If

        Dim result As STUN_Result = Query(stunServer, port, CreateSocket(New IPEndPoint(localIP, 0), ProtocolType.Udp))
        If result.PublicEndPoint IsNot Nothing Then
            Return result.PublicEndPoint.Address
        Else
            Throw New IOException("Failed to STUN public IP address. STUN server name is invalid or firewall blocks STUN.")
        End If
    End Function

#End Region

#Region "method GetPublicEP"

    ''' <summary>
    ''' Resolves socket local end point to public end point.
    ''' </summary>
    ''' <param name="stunServer">STUN server.</param>
    ''' <param name="port">STUN server port. Default port is 3478.</param>
    ''' <param name="socket">UDP socket to use.</param>
    ''' <returns>Returns public IP end point.</returns>
    ''' <exception cref="ArgumentNullException">Is raised when <b>stunServer</b> or <b>socket</b> is null reference.</exception>
    ''' <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
    ''' <exception cref="IOException">Is raised when no connection to STUN server.</exception>
    Public Shared Function GetPublicEP(stunServer As String, port As Integer, socket As Socket) As IPEndPoint
        If stunServer Is Nothing Then
            Throw New ArgumentNullException("stunServer")
        End If
        If stunServer = "" Then
            Throw New ArgumentException("Argument 'stunServer' value must be specified.")
        End If
        If port < 1 Then
            Throw New ArgumentException("Invalid argument 'port' value.")
        End If
        If socket Is Nothing Then
            Throw New ArgumentNullException("socket")
        End If
        If socket.ProtocolType <> ProtocolType.Udp Then
            Throw New ArgumentException("Socket must be UDP socket !")
        End If

        Dim remoteEndPoint As New IPEndPoint(System.Net.Dns.GetHostAddresses(stunServer)(0), port)

        Try
            ' Test I
            Dim test1 As New STUN_Message()
            test1.Type = STUN_MessageType.BindingRequest
            Dim test1response As STUN_Message = DoTransaction(test1, socket, remoteEndPoint, 1000)

            ' UDP blocked.
            If test1response Is Nothing Then
                Throw New IOException("Failed to STUN public IP address. STUN server name is invalid or firewall blocks STUN.")
            End If

            Return test1response.SourceAddress
        Catch
            Throw New IOException("Failed to STUN public IP address. STUN server name is invalid or firewall blocks STUN.")
        Finally
            ' Junk all late responses.
            Dim startTime As DateTime = DateTime.Now
            While startTime.AddMilliseconds(200) > DateTime.Now
                ' We got response.
                If socket.Poll(1, SelectMode.SelectRead) Then
                    Dim receiveBuffer As Byte() = New Byte(511) {}
                    socket.Receive(receiveBuffer)
                End If
            End While
        End Try
    End Function

#End Region


#Region "method GetSharedSecret"

    Private Sub GetSharedSecret()
        '
        '                *) Open TLS connection to STUN server.
        '                *) Send Shared Secret request.
        '            


        '
        '            using(SocketEx socket = new SocketEx()){
        '                socket.RawSocket.ReceiveTimeout = 5000;
        '                socket.RawSocket.SendTimeout = 5000;
        '
        '                socket.Connect(host,port);
        '                socket.SwitchToSSL_AsClient();                
        '
        '                // Send Shared Secret request.
        '                STUN_Message sharedSecretRequest = new STUN_Message();
        '                sharedSecretRequest.Type = STUN_MessageType.SharedSecretRequest;
        '                socket.Write(sharedSecretRequest.ToByteData());
        '                
        '                // TODO: Parse message
        '
        '                // We must get  "Shared Secret" or "Shared Secret Error" response.
        '
        '                byte[] receiveBuffer = new byte[256];
        '                socket.RawSocket.Receive(receiveBuffer);
        '
        '                STUN_Message sharedSecretRequestResponse = new STUN_Message();
        '                if(sharedSecretRequestResponse.Type == STUN_MessageType.SharedSecretResponse){
        '                }
        '                // Shared Secret Error or Unknown response, just try again.
        '                else{
        '                    // TODO: Unknown response
        '                }
        '            }

    End Sub

#End Region

#Region "method DoTransaction"

    ''' <summary>
    ''' Does STUN transaction. Returns transaction response or null if transaction failed.
    ''' </summary>
    ''' <param name="request">STUN message.</param>
    ''' <param name="socket">Socket to use for send/receive.</param>
    ''' <param name="remoteEndPoint">Remote end point.</param>
    ''' <param name="timeout">Timeout in milli seconds.</param>
    ''' <returns>Returns transaction response or null if transaction failed.</returns>
    Private Shared Function DoTransaction(request As STUN_Message, socket As Socket, remoteEndPoint As IPEndPoint, timeout As Integer) As STUN_Message
        Dim requestBytes As Byte() = request.ToByteData()
        Dim startTime As DateTime = DateTime.Now
        ' Retransmit with 500 ms.
        While startTime.AddMilliseconds(timeout) > DateTime.Now
            Try
                socket.SendTo(requestBytes, remoteEndPoint)

                ' We got response.
                If socket.Poll(500 * 1000, SelectMode.SelectRead) Then
                    Dim receiveBuffer As Byte() = New Byte(511) {}
                    socket.Receive(receiveBuffer)

                    ' Parse message
                    Dim response As New STUN_Message()
                    response.Parse(receiveBuffer)

                    ' Check that transaction ID matches or not response what we want.
                    If CompareArray(request.TransactionID, response.TransactionID) Then
                        Return response
                    End If
                End If
            Catch
            End Try
        End While

        Return Nothing
    End Function

#End Region


    ' TODO: Update to RFC 5389

End Class







''' <summary>
''' This class implements STUN ERROR-CODE. Defined in RFC 3489 11.2.9.
''' </summary>
Public Class STUN_t_ErrorCode
    Private m_Code As Integer = 0
    Private m_ReasonText As String = ""

    ''' <summary>
    ''' Default constructor.
    ''' </summary>
    ''' <param name="code">Error code.</param>
    ''' <param name="reasonText">Reason text.</param>
    Public Sub New(code As Integer, reasonText As String)
        m_Code = code
        m_ReasonText = reasonText
    End Sub


#Region "Properties Implementation"

    ''' <summary>
    ''' Gets or sets error code.
    ''' </summary>
    Public Property Code() As Integer
        Get
            Return m_Code
        End Get

        Set(value As Integer)
            m_Code = value
        End Set
    End Property

    ''' <summary>
    ''' Gets reason text.
    ''' </summary>
    Public Property ReasonText() As String
        Get
            Return m_ReasonText
        End Get

        Set(value As String)
            m_ReasonText = value
        End Set
    End Property

#End Region

End Class







''' <summary>
''' This class implements STUN CHANGE-REQUEST attribute. Defined in RFC 3489 11.2.4.
''' </summary>
Public Class STUN_t_ChangeRequest
    Private m_ChangeIP As Boolean = True
    Private m_ChangePort As Boolean = True

    ''' <summary>
    ''' Default constructor.
    ''' </summary>
    Public Sub New()
    End Sub

    ''' <summary>
    ''' Default constructor.
    ''' </summary>
    ''' <param name="changeIP">Specifies if STUN server must send response to different IP than request was received.</param>
    ''' <param name="changePort">Specifies if STUN server must send response to different port than request was received.</param>
    Public Sub New(changeIP As Boolean, changePort As Boolean)
        m_ChangeIP = changeIP
        m_ChangePort = changePort
    End Sub


#Region "Properties Implementation"

    ''' <summary>
    ''' Gets or sets if STUN server must send response to different IP than request was received.
    ''' </summary>
    Public Property ChangeIP() As Boolean
        Get
            Return m_ChangeIP
        End Get

        Set(value As Boolean)
            m_ChangeIP = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets if STUN server must send response to different port than request was received.
    ''' </summary>
    Public Property ChangePort() As Boolean
        Get
            Return m_ChangePort
        End Get

        Set(value As Boolean)
            m_ChangePort = value
        End Set
    End Property

#End Region

End Class







''' <summary>
''' This enum specifies STUN message type.
''' </summary>
Public Enum STUN_MessageType
    ''' <summary>
    ''' STUN message is binding request.
    ''' </summary>
    BindingRequest = &H1

    ''' <summary>
    ''' STUN message is binding request response.
    ''' </summary>
    BindingResponse = &H101

    ''' <summary>
    ''' STUN message is binding requesr error response.
    ''' </summary>
    BindingErrorResponse = &H111

    ''' <summary>
    ''' STUN message is "shared secret" request.
    ''' </summary>
    SharedSecretRequest = &H2

    ''' <summary>
    ''' STUN message is "shared secret" request response.
    ''' </summary>
    SharedSecretResponse = &H102

    ''' <summary>
    ''' STUN message is "shared secret" request error response.
    ''' </summary>
    SharedSecretErrorResponse = &H112
End Enum






''' <summary>
''' Implements STUN message. Defined in RFC 3489.
''' </summary>
Public Class STUN_Message
#Region "enum AttributeType"

    ''' <summary>
    ''' Specifies STUN attribute type.
    ''' </summary>
    Private Enum AttributeType
        MappedAddress = &H1
        ResponseAddress = &H2
        ChangeRequest = &H3
        SourceAddress = &H4
        ChangedAddress = &H5
        Username = &H6
        Password = &H7
        MessageIntegrity = &H8
        ErrorCode = &H9
        UnknownAttribute = &HA
        ReflectedFrom = &HB
        XorMappedAddress = &H8020
        XorOnly = &H21
        ServerName = &H8022
    End Enum

#End Region

#Region "enum IPFamily"

    ''' <summary>
    ''' Specifies IP address family.
    ''' </summary>
    Private Enum IPFamily
        IPv4 = &H1
        IPv6 = &H2
    End Enum

#End Region

    Private m_Type As STUN_MessageType = STUN_MessageType.BindingRequest
    Private m_MagicCookie As Integer = 0
    Private m_pTransactionID As Byte() = Nothing
    Private m_pMappedAddress As IPEndPoint = Nothing
    Private m_pResponseAddress As IPEndPoint = Nothing
    Private m_pChangeRequest As STUN_t_ChangeRequest = Nothing
    Private m_pSourceAddress As IPEndPoint = Nothing
    Private m_pChangedAddress As IPEndPoint = Nothing
    Private m_UserName As String = Nothing
    Private m_Password As String = Nothing
    Private m_pErrorCode As STUN_t_ErrorCode = Nothing
    Private m_pReflectedFrom As IPEndPoint = Nothing
    Private m_ServerName As String = Nothing

    ''' <summary>
    ''' Default constructor.
    ''' </summary>
    Public Sub New()
        m_pTransactionID = New Byte(11) {}
        Dim rand As New Random()
        rand.NextBytes(m_pTransactionID)
    End Sub


#Region "method Parse"

    ''' <summary>
    ''' Parses STUN message from raw data packet.
    ''' </summary>
    ''' <param name="data">Raw STUN message.</param>
    ''' <exception cref="ArgumentNullException">Is raised when <b>data</b> is null reference.</exception>
    Public Sub Parse(data As Byte())
        If data Is Nothing Then
            Throw New ArgumentNullException("data")
        End If

        ' RFC 5389 6.             
        '                All STUN messages MUST start with a 20-byte header followed by zero
        '                or more Attributes.  The STUN header contains a STUN message type,
        '                magic cookie, transaction ID, and message length.
        '
        '                 0                   1                   2                   3
        '                 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        '                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                 |0 0|     STUN Message Type     |         Message Length        |
        '                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                 |                         Magic Cookie                          |
        '                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                 |                                                               |
        '                 |                     Transaction ID (96 bits)                  |
        '                 |                                                               |
        '                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '              
        '               The message length is the count, in bytes, of the size of the
        '               message, not including the 20 byte header.
        '            


        If data.Length < 20 Then
            Throw New ArgumentException("Invalid STUN message value !")
        End If

        Dim offset As Integer = 0

        '--- message header --------------------------------------------------

        ' STUN Message Type
        Dim messageType As Integer = (data(offset) << 8 Or data(offset + 1))
        If messageType = CInt(STUN_MessageType.BindingErrorResponse) Then
            m_Type = STUN_MessageType.BindingErrorResponse
        ElseIf messageType = CInt(STUN_MessageType.BindingRequest) Then
            m_Type = STUN_MessageType.BindingRequest
        ElseIf messageType = CInt(STUN_MessageType.BindingResponse) Then
            m_Type = STUN_MessageType.BindingResponse
        ElseIf messageType = CInt(STUN_MessageType.SharedSecretErrorResponse) Then
            m_Type = STUN_MessageType.SharedSecretErrorResponse
        ElseIf messageType = CInt(STUN_MessageType.SharedSecretRequest) Then
            m_Type = STUN_MessageType.SharedSecretRequest
        ElseIf messageType = CInt(STUN_MessageType.SharedSecretResponse) Then
            m_Type = STUN_MessageType.SharedSecretResponse
        Else
            Throw New ArgumentException("Invalid STUN message type value !")
        End If

        ' Message Length
        Dim messageLength As Integer = (data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 8 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)))

        ' Magic Cookie
        m_MagicCookie = (data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 24 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 16 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 8 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)))

        ' Transaction ID
        m_pTransactionID = New Byte(11) {}
        Array.Copy(data, offset, m_pTransactionID, 0, 12)
        offset += 12

        '--- Message attributes ---------------------------------------------
        While (offset - 20) < messageLength
            ParseAttribute(data, offset)
        End While
    End Sub

#End Region

#Region "method ToByteData"

    ''' <summary>
    ''' Converts this to raw STUN packet.
    ''' </summary>
    ''' <returns>Returns raw STUN packet.</returns>
    Public Function ToByteData() As Byte()
        ' RFC 5389 6.             
        '                All STUN messages MUST start with a 20-byte header followed by zero
        '                or more Attributes.  The STUN header contains a STUN message type,
        '                magic cookie, transaction ID, and message length.
        '
        '                 0                   1                   2                   3
        '                 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        '                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                 |0 0|     STUN Message Type     |         Message Length        |
        '                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                 |                         Magic Cookie                          |
        '                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                 |                                                               |
        '                 |                     Transaction ID (96 bits)                  |
        '                 |                                                               |
        '                 +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '              
        '               The message length is the count, in bytes, of the size of the
        '               message, not including the 20 byte header.
        '            


        ' We allocate 512 for header, that should be more than enough.
        Dim msg As Byte() = New Byte(511) {}

        Dim offset As Integer = 0

        '--- message header -------------------------------------

        ' STUN Message Type (2 bytes)
        msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte((CInt(Me.Type) >> 8) And &H3F)
        msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(CInt(Me.Type) And &HFF)

        ' Message Length (2 bytes) will be assigned at last.
        msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
        msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0

        ' Magic Cookie           
        msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte((Me.MagicCookie >> 24) And &HFF)
        msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte((Me.MagicCookie >> 16) And &HFF)
        msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte((Me.MagicCookie >> 8) And &HFF)
        msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte((Me.MagicCookie >> 0) And &HFF)

        ' Transaction ID (16 bytes)
        Array.Copy(m_pTransactionID, 0, msg, offset, 12)
        offset += 12

        '--- Message attributes ------------------------------------

        ' RFC 3489 11.2.
        '                After the header are 0 or more attributes.  Each attribute is TLV
        '                encoded, with a 16 bit type, 16 bit length, and variable value:
        '
        '                0                   1                   2                   3
        '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '               |         Type                  |            Length             |
        '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '               |                             Value                             ....
        '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '            


        If Me.MappedAddress IsNot Nothing Then
            StoreEndPoint(AttributeType.MappedAddress, Me.MappedAddress, msg, offset)
        ElseIf Me.ResponseAddress IsNot Nothing Then
            StoreEndPoint(AttributeType.ResponseAddress, Me.ResponseAddress, msg, offset)
        ElseIf Me.ChangeRequest IsNot Nothing Then
            '
            '                    The CHANGE-REQUEST attribute is used by the client to request that
            '                    the server use a different address and/or port when sending the
            '                    response.  The attribute is 32 bits long, although only two bits (A
            '                    and B) are used:
            '
            '                     0                   1                   2                   3
            '                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '                    |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '
            '                    The meaning of the flags is:
            '
            '                    A: This is the "change IP" flag.  If true, it requests the server
            '                       to send the Binding Response with a different IP address than the
            '                       one the Binding Request was received on.
            '
            '                    B: This is the "change port" flag.  If true, it requests the
            '                       server to send the Binding Response with a different port than the
            '                       one the Binding Request was received on.
            '                


            ' Attribute header
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.ChangeRequest) >> 8
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.ChangeRequest) And &HFF
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 4

            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(Convert.ToInt32(Me.ChangeRequest.ChangeIP) << 2 Or Convert.ToInt32(Me.ChangeRequest.ChangePort) << 1)
        ElseIf Me.SourceAddress IsNot Nothing Then
            StoreEndPoint(AttributeType.SourceAddress, Me.SourceAddress, msg, offset)
        ElseIf Me.ChangedAddress IsNot Nothing Then
            StoreEndPoint(AttributeType.ChangedAddress, Me.ChangedAddress, msg, offset)
        ElseIf Me.UserName IsNot Nothing Then
            Dim userBytes As Byte() = Encoding.ASCII.GetBytes(Me.UserName)

            ' Attribute header
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.Username) >> 8
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.Username) And &HFF
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(userBytes.Length >> 8)
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(userBytes.Length And &HFF)

            Array.Copy(userBytes, 0, msg, offset, userBytes.Length)
            offset += userBytes.Length
        ElseIf Me.Password IsNot Nothing Then
            Dim userBytes As Byte() = Encoding.ASCII.GetBytes(Me.UserName)

            ' Attribute header
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.Password) >> 8
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.Password) And &HFF
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(userBytes.Length >> 8)
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(userBytes.Length And &HFF)

            Array.Copy(userBytes, 0, msg, offset, userBytes.Length)
            offset += userBytes.Length
        ElseIf Me.ErrorCode IsNot Nothing Then
            ' 3489 11.2.9.
            '                    0                   1                   2                   3
            '                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '                    |                   0                     |Class|     Number    |
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '                    |      Reason Phrase (variable)                                ..
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '                


            Dim reasonBytes As Byte() = Encoding.ASCII.GetBytes(Me.ErrorCode.ReasonText)

            ' Header
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CInt(AttributeType.ErrorCode)
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(4 + reasonBytes.Length)

            ' Empty
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
            ' Class
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(Math.Floor(CDbl(Me.ErrorCode.Code / 100)))
            ' Number
            msg(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(Me.ErrorCode.Code And &HFF)
            ' ReasonPhrase
            Array.Copy(reasonBytes, msg, reasonBytes.Length)
            offset += reasonBytes.Length
        ElseIf Me.ReflectedFrom IsNot Nothing Then
            StoreEndPoint(AttributeType.ReflectedFrom, Me.ReflectedFrom, msg, offset)
        End If

        ' Update Message Length. NOTE: 20 bytes header not included.
        msg(2) = CByte((offset - 20) >> 8)
        msg(3) = CByte((offset - 20) And &HFF)

        ' Make reatval with actual size.
        Dim retVal As Byte() = New Byte(offset - 1) {}
        Array.Copy(msg, retVal, retVal.Length)

        Return retVal
    End Function

#End Region


#Region "method ParseAttribute"

    ''' <summary>
    ''' Parses attribute from data.
    ''' </summary>
    ''' <param name="data">SIP message data.</param>
    ''' <param name="offset">Offset in data.</param>
    Private Sub ParseAttribute(data As Byte(), ByRef offset As Integer)
        ' RFC 3489 11.2.
        '                Each attribute is TLV encoded, with a 16 bit type, 16 bit length, and variable value:
        '
        '                0                   1                   2                   3
        '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '               |         Type                  |            Length             |
        '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '               |                             Value                             ....
        '               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+                            
        '            


        ' Type
        Dim type As AttributeType = CType(data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 8 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)), AttributeType)

        ' Length
        Dim length As Integer = (data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 8 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)))

        ' MAPPED-ADDRESS
        If type = AttributeType.MappedAddress Then
            m_pMappedAddress = ParseEndPoint(data, offset)
            ' RESPONSE-ADDRESS
        ElseIf type = AttributeType.ResponseAddress Then
            m_pResponseAddress = ParseEndPoint(data, offset)
            ' CHANGE-REQUEST
        ElseIf type = AttributeType.ChangeRequest Then
            '
            '                    The CHANGE-REQUEST attribute is used by the client to request that
            '                    the server use a different address and/or port when sending the
            '                    response.  The attribute is 32 bits long, although only two bits (A
            '                    and B) are used:
            '
            '                     0                   1                   2                   3
            '                     0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '                    |0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 A B 0|
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '
            '                    The meaning of the flags is:
            '
            '                    A: This is the "change IP" flag.  If true, it requests the server
            '                       to send the Binding Response with a different IP address than the
            '                       one the Binding Request was received on.
            '
            '                    B: This is the "change port" flag.  If true, it requests the
            '                       server to send the Binding Response with a different port than the
            '                       one the Binding Request was received on.
            '                


            ' Skip 3 bytes
            offset += 3

            m_pChangeRequest = New STUN_t_ChangeRequest((data(offset) And 4) <> 0, (data(offset) And 2) <> 0)
            offset += 1
            ' SOURCE-ADDRESS
        ElseIf type = AttributeType.SourceAddress Then
            m_pSourceAddress = ParseEndPoint(data, offset)
            ' CHANGED-ADDRESS
        ElseIf type = AttributeType.ChangedAddress Then
            m_pChangedAddress = ParseEndPoint(data, offset)
            ' USERNAME
        ElseIf type = AttributeType.Username Then
            m_UserName = Encoding.[Default].GetString(data, offset, length)
            offset += length
            ' PASSWORD
        ElseIf type = AttributeType.Password Then
            m_Password = Encoding.[Default].GetString(data, offset, length)
            offset += length
            ' MESSAGE-INTEGRITY
        ElseIf type = AttributeType.MessageIntegrity Then
            offset += length
            ' ERROR-CODE
        ElseIf type = AttributeType.ErrorCode Then
            ' 3489 11.2.9.
            '                    0                   1                   2                   3
            '                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '                    |                   0                     |Class|     Number    |
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '                    |      Reason Phrase (variable)                                ..
            '                    +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            '                


            Dim errorCode As Integer = (data(offset + 2) And &H7) * 100 + (data(offset + 3) And &HFF)

            m_pErrorCode = New STUN_t_ErrorCode(errorCode, Encoding.[Default].GetString(data, offset + 4, length - 4))
            offset += length
            ' UNKNOWN-ATTRIBUTES
        ElseIf type = AttributeType.UnknownAttribute Then
            offset += length
            ' REFLECTED-FROM
        ElseIf type = AttributeType.ReflectedFrom Then
            m_pReflectedFrom = ParseEndPoint(data, offset)
            ' XorMappedAddress
            ' XorOnly
            ' ServerName
        ElseIf type = AttributeType.ServerName Then
            m_ServerName = Encoding.[Default].GetString(data, offset, length)
            offset += length
        Else
            ' Unknown
            offset += length
        End If
    End Sub

#End Region

#Region "method ParseEndPoint"

    ''' <summary>
    ''' Pasrses IP endpoint attribute.
    ''' </summary>
    ''' <param name="data">STUN message data.</param>
    ''' <param name="offset">Offset in data.</param>
    ''' <returns>Returns parsed IP end point.</returns>
    Private Function ParseEndPoint(data As Byte(), ByRef offset As Integer) As IPEndPoint
        '
        '                It consists of an eight bit address family, and a sixteen bit
        '                port, followed by a fixed length value representing the IP address.
        '
        '                0                   1                   2                   3
        '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                |x x x x x x x x|    Family     |           Port                |
        '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                |                             Address                           |
        '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '            


        ' Skip family
        offset += 1
        offset += 1

        ' Port
        Dim port As Integer = (data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) << 8 Or data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)))

        ' Address
        Dim ip As Byte() = New Byte(3) {}
        ip(0) = data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1))
        ip(1) = data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1))
        ip(2) = data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1))
        ip(3) = data(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1))

        Return New IPEndPoint(New IPAddress(ip), port)
    End Function

#End Region

#Region "method StoreEndPoint"

    ''' <summary>
    ''' Stores ip end point attribute to buffer.
    ''' </summary>
    ''' <param name="type">Attribute type.</param>
    ''' <param name="endPoint">IP end point.</param>
    ''' <param name="message">Buffer where to store.</param>
    ''' <param name="offset">Offset in buffer.</param>
    Private Sub StoreEndPoint(type As AttributeType, endPoint As IPEndPoint, message As Byte(), ByRef offset As Integer)
        '
        '                It consists of an eight bit address family, and a sixteen bit
        '                port, followed by a fixed length value representing the IP address.
        '
        '                0                   1                   2                   3
        '                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
        '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                |x x x x x x x x|    Family     |           Port                |
        '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        '                |                             Address                           |
        '                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+             
        '            


        ' Header
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(CInt(type) >> 8)
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(CInt(type) And &HFF)
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 8

        ' Unused
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = 0
        ' Family
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(IPFamily.IPv4)
        ' Port
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(endPoint.Port >> 8)
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = CByte(endPoint.Port And &HFF)
        ' Address
        Dim ipBytes As Byte() = endPoint.Address.GetAddressBytes()
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = ipBytes(0)
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = ipBytes(1)
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = ipBytes(2)
        message(System.Math.Max(System.Threading.Interlocked.Increment(offset), offset - 1)) = ipBytes(3)
    End Sub

#End Region


#Region "Properties Implementation"

    ''' <summary>
    ''' Gets STUN message type.
    ''' </summary>
    Public Property Type() As STUN_MessageType
        Get
            Return m_Type
        End Get

        Set(value As STUN_MessageType)
            m_Type = value
        End Set
    End Property

    ''' <summary>
    ''' Gets magic cookie value. This is always 0x2112A442.
    ''' </summary>
    Public ReadOnly Property MagicCookie() As Integer
        Get
            Return m_MagicCookie
        End Get
    End Property

    ''' <summary>
    ''' Gets transaction ID.
    ''' </summary>
    Public ReadOnly Property TransactionID() As Byte()
        Get
            Return m_pTransactionID
        End Get
    End Property

    ''' <summary>
    ''' Gets or sets IP end point what was actually connected to STUN server. Returns null if not specified.
    ''' </summary>
    Public Property MappedAddress() As IPEndPoint
        Get
            Return m_pMappedAddress
        End Get

        Set(value As IPEndPoint)
            m_pMappedAddress = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets IP end point where to STUN client likes to receive response.
    ''' Value null means not specified.
    ''' </summary>
    Public Property ResponseAddress() As IPEndPoint
        Get
            Return m_pResponseAddress
        End Get

        Set(value As IPEndPoint)
            m_pResponseAddress = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets how and where STUN server must send response back to STUN client.
    ''' Value null means not specified.
    ''' </summary>
    Public Property ChangeRequest() As STUN_t_ChangeRequest
        Get
            Return m_pChangeRequest
        End Get

        Set(value As STUN_t_ChangeRequest)
            m_pChangeRequest = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets STUN server IP end point what sent response to STUN client. Value null
    ''' means not specified.
    ''' </summary>
    Public Property SourceAddress() As IPEndPoint
        Get
            Return m_pSourceAddress
        End Get

        Set(value As IPEndPoint)
            m_pSourceAddress = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets IP end point where STUN server will send response back to STUN client 
    ''' if the "change IP" and "change port" flags had been set in the ChangeRequest.
    ''' </summary>
    Public Property ChangedAddress() As IPEndPoint
        Get
            Return m_pChangedAddress
        End Get

        Set(value As IPEndPoint)
            m_pChangedAddress = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets user name. Value null means not specified.
    ''' </summary>          
    Public Property UserName() As String
        Get
            Return m_UserName
        End Get

        Set(value As String)
            m_UserName = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets password. Value null means not specified.
    ''' </summary>
    Public Property Password() As String
        Get
            Return m_Password
        End Get

        Set(value As String)
            m_Password = value
        End Set
    End Property

    'public MessageIntegrity

    ''' <summary>
    ''' Gets or sets error info. Returns null if not specified.
    ''' </summary>
    Public Property ErrorCode() As STUN_t_ErrorCode
        Get
            Return m_pErrorCode
        End Get

        Set(value As STUN_t_ErrorCode)
            m_pErrorCode = value
        End Set
    End Property


    ''' <summary>
    ''' Gets or sets IP endpoint from which IP end point STUN server got STUN client request.
    ''' Value null means not specified.
    ''' </summary>
    Public Property ReflectedFrom() As IPEndPoint
        Get
            Return m_pReflectedFrom
        End Get

        Set(value As IPEndPoint)
            m_pReflectedFrom = value
        End Set
    End Property

    ''' <summary>
    ''' Gets or sets server name.
    ''' </summary>
    Public Property ServerName() As String
        Get
            Return m_ServerName
        End Get

        Set(value As String)
            m_ServerName = value
        End Set
    End Property

#End Region


    '=======================================================
    'Service provided by Telerik (www.telerik.com)
    'Conversion powered by NRefactory.
    'Twitter: @telerik
    'Facebook: facebook.com/telerik
    '=======================================================

End Class
