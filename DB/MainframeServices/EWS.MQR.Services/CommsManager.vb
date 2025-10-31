Imports System.IO
Imports System.Text
Imports System.Xml
Imports System.Xml.Serialization

Imports EasyTcp4
Imports EasyTcp4.ClientUtils
Imports EasyTcp4.PacketUtils
Imports EasyTcp4.ServerUtils

Imports EWS.Diagnostics

Imports EWS.MQR.XML

Public Class CommsManager
    private _tcpServer as EasyTcpServer

    Public Sub Initialise()
        InitialiseTcpListener()
    End Sub

    Public Sub Shutdown()
        ShutdownTcpListener()
    End Sub

    private Sub InitialiseTcpListener()
        Try
            Trace.WriteLine("Initialising TcpListener on " & MQRConfig.Current.MQRServiceConfig.TcpRequestListeningPort, TraceLevel.Verbose)
            _tcpServer = New EasyTcpServer()
            _tcpServer.Serialize = AddressOf Serialize
            _tcpServer.Deserialize = AddressOf Deserialize

            AddHandler _tcpServer.OnDataReceive, AddressOf TcpServerDataReceived
            _tcpServer.Start(MQRConfig.Current.MQRServiceConfig.TcpRequestListeningPort)
        Catch e As Exception
            EventLogging.Log("Exception whilst Initialising TcpListener - " & e.ToString, Me.GetType.Name, EventLogEntryType.Error)
        End Try
    End Sub

    Private Sub TcpServerDataReceived(sender As Object, e As Message)
        Dim compressionEnabled as Boolean = e.IsCompressed()

        Try
            Trace.WriteLine("Received Compressed: " & compressionEnabled & " Data: " & e.ToString(), TraceLevel.Verbose)
            Dim request as QueryRequest

            If compressionEnabled Then
                request = e.Decompress().To (Of QueryRequest)()
            Else
                request = e.To (of queryRequest)()
            End If

            Dim response as QueryResult = RequestService.ProcessRequest(request)
            SendResponse(e.Client, response, compressionEnabled)

        Catch ex As Exception
            EventLogging.Log("Exception whilst Processing TcpListener Data - " & ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
            Dim errorField as new QueryResultField()
            errorField.Identifier = "Error"
            errorField.Value = ex.ToString()
            Dim errorRow as new QueryResultRow()
            errorRow.Identifier = "Error"
            errorRow.Fields.Add(errorField)
            Dim errorSection as new QueryResultSection()
            errorSection.Identifier = "Error"
            errorSection.AddRow(errorRow)
            Dim errorResult as new QueryResult()
            errorResult.Sections.Add("Error", errorSection)
            
            SendResponse(e.Client, errorResult, compressionEnabled)
        End Try
    End Sub

    Private Sub SendResponse(client As EasyTcpClient, response As QueryResult, compressionEnabled As Boolean)
        Try
            Trace.WriteLine("Sending Data to " & client.GetIp().ToString() & " Compressed:" & compressionEnabled & " Sections:" & response.Sections.Count, TraceLevel.Verbose)
            client.Send(response, compressionEnabled)
        Catch ex As Exception
            EventLogging.Log("Exception whilst sending QueryResult - " & ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
        End Try
    End Sub

    Private Function Deserialize(ByVal arg1 As Byte(), ByVal arg2 As Type) As Object
        Dim stream as MemoryStream = New MemoryStream()
        stream.Write(arg1, 0, arg1.Length)
        stream.Position = 0
        Dim xs as XmlSerializer = New XmlSerializer(GetType(QueryRequest))
        Return xs.Deserialize(stream)
    End Function

    Private Function Serialize(ByVal arg As Object) As Byte()
        Dim memoryStream as MemoryStream = New MemoryStream()
        Dim xs as XmlSerializer = New XmlSerializer(GetType(QueryResult))
        Dim xmlTextWriter as XmlTextWriter = New XmlTextWriter(memoryStream, Encoding.UTF8)
        xs.Serialize(xmlTextWriter, arg)
        memoryStream = CType(xmlTextWriter.BaseStream, MemoryStream)
        Return memoryStream.ToArray()
    End Function

    private Sub ShutdownTcpListener()
        Try
            If not _tcpServer is Nothing Then
                _tcpServer.Dispose()
                _tcpServer = Nothing
            End If
        Catch e As Exception
            EventLogging.Log("Exception whilst Stopping TcpListener - " & e.ToString, Me.GetType.Name, EventLogEntryType.Error)
        End Try
    End Sub
End Class
