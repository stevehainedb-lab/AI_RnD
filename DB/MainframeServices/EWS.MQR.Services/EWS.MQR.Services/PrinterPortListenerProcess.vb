Imports System
Imports System.Collections.Specialized
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Net
Imports EWS.MQR.XML
Imports System.Net.Sockets
Imports System.Threading
Imports EWS.Diagnostics
Imports System.Text

Friend Class PrinterPortListenerProcess
    Public Event OnReceivedData(ByVal Sender As PrinterPortListenerProcess, ByVal Data As ReceivedDataEventArgs)

    Private Shared ReadOnly IpMatcher As New IPMatcher
    Private Shared ReadOnly HostName As String
    Private Shared ReadOnly LocalAddr As IPAddress

    Private readonly _port As Integer
    Private _run As Boolean
    private _server As TcpListener

    Private _listening As Boolean
    Private _documentsReceived As Integer

    Shared Sub New()

        If MQRConfig.Current.MQRServiceConfig.PrintQueueServer.ToLower() = "localhost" Then
            HostName = "localhost"
            LocalAddr = IPAddress.Loopback
        Else
            HostName = Dns.GetHostName
            For Each Address As IPAddress In Dns.GetHostEntry(HostName).AddressList
                If Address.AddressFamily = AddressFamily.InterNetwork Then
                    LocalAddr = Address
                    Exit For
                End If
            Next
        End If
        Trace.WriteLine("Printer Port Listener will listen on " + LocalAddr.ToString, TraceLevel.Verbose)

        Dim AllowedIPs As String() = MQRConfig.Current.MQRServiceConfig.PrinterHostsAllowed.Split(","c)
        For Each AllowedIP As String In AllowedIPs
            IpMatcher.Add(AllowedIP.Trim)
        Next
    End Sub

    Public ReadOnly Property Listening() As Boolean
        Get
            Return _listening
        End Get
    End Property

    Public ReadOnly Property StoppingOrStopped() As Boolean
        Get
            Return Not _run
        End Get
    End Property

    Public Sub New(ByVal Port As Integer)

        _port = Port
        NotificationData.SetValue(ToString, ProcessState.Stopped.ToString)
        'NotificationData.SetDelegate(ToString, New GetNotifyDataValue(AddressOf DocumentsReceived))
    End Sub

    Public Sub StartListening()
        If Not _run Then
            _run = True
            ThreadManager.Thread(ToString(), New ThreadManager.DoWork(AddressOf ListenThread), ThreadPriority.Normal, True, True)
        End If
    End Sub

    Public Sub StopListening()
        _run = False
        _server.Stop()
        ThreadManager.ShutdownThread(ToString)
    End Sub

    Const BUFFER_SIZE_K As Integer = 1

    Private Function ListenThread() As Boolean

        Dim m_InternalID As Guid = Guid.NewGuid
        Try

            Trace.WriteLine(ToString() & " Started Listening. ID = " + m_InternalID.ToString, TraceLevel.Verbose)
            _server = New TcpListener(LocalAddr, _port)
            _server.Start()

            NotificationData.SetValue(ToString, ProcessState.WaitingForConnection.ToString)
            Dim Buffer((1024*BUFFER_SIZE_K) - 1) As Byte

            While _run
                _listening = True
                Trace.WriteLine(ToString() & " Waiting for connection.", TraceLevel.Verbose)

                If _run Then
                    Dim tc As TcpClient = Nothing

                    Try
                        Trace.WriteLine(ToString() & " About to try accepting tcp client on " + _server.LocalEndpoint.ToString + " ID = " + m_InternalID.ToString, TraceLevel.Verbose)

                        tc = _server.AcceptTcpClient
                        Trace.WriteLine(ToString() & " Successfully accepted tcp client.", TraceLevel.Verbose)
                        Dim ipaddress As String = tc.Client.RemoteEndPoint.ToString
                        ipaddress = ipaddress.Substring(0, ipaddress.IndexOf(":"))
                        If Not IpMatcher.Match(ipaddress) Then
                            Trace.WriteLine(ToString() & " Rejected IP NOT Authorised: " + ipaddress & ".", TraceLevel.Warning)
                            tc.Close()
                        Else
                            Trace.WriteLine(ToString() & " Client connected: " + tc.Client.RemoteEndPoint.ToString & ".", TraceLevel.Verbose)
                            NotificationData.SetValue(ToString, ProcessState.ReceivingData.ToString)

                            Dim RawDataBuilder As New StringBuilder

                            Dim DataStream As NetworkStream = tc.GetStream

                            Dim Read As Integer
                            Dim DataToAdd As String

                            Do
                                Read = DataStream.Read(Buffer, 0, Buffer.Length)

                                DataToAdd = Encoding.ASCII.GetString(Buffer, 0, Read)
                                DataToAdd = Replace(DataToAdd, ControlChars.FormFeed, ControlChars.NewLine)
                                RawDataBuilder.Append(DataToAdd)

                                Trace.WriteLine(ToString() & " buffered " & Read.ToString & " Bytes.", TraceLevel.Verbose)

                            Loop While Read <> 0

                            DataStream.Close()
                            tc.Close()

                            Trace.WriteLine(ToString() & " Received " & RawDataBuilder.Length & " Bytes ", TraceLevel.Verbose)

                            Dim Data As String = RawDataBuilder.ToString.Trim

                            If Data.Length > 1 Then
                                'Ok the Printer has closed and we have data but have we got more than 1 print job?
                                'The way to tell is to see if he have a header line.
                                For Each Response As String In GetResponses(Data.ToString)
                                    Thread.Sleep(200)
                                    FileWriter.Write("PrinterPortListener", " Received:" & vbCrLf & Response.ToString.Trim, _port.ToString)
                                    _documentsReceived += 1
                                    RaiseEvent OnReceivedData(Me, New ReceivedDataEventArgs(Response.ToString.Trim))
                                Next
                            End If

                            NotificationData.SetValue(ToString, ProcessState.WaitingForConnection.ToString)
                        End If

                    Catch ex as SocketException
                        If Not ex.Message = "A blocking operation was interrupted by a call to WSACancelBlockingCall" Then
                            EventLogging.Log("EXCEPTION:" & ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
                        End If
                        
                    Catch ex As Exception
                        EventLogging.Log("EXCEPTION:" & ex.ToString, Me.GetType.Name, EventLogEntryType.Error)

                        If Not tc Is Nothing Then
                            If tc.Connected Then
                                tc.Close()
                            End If
                        End If
                    End Try
                End If
                MQRManager.EMailAlertPassed("Shutdown" & ToString())
            End While
        Catch ex As ThreadAbortException
            Trace.WriteLine(ToString() & " Threading Exception.", TraceLevel.Verbose)
            If _run Then
                EventLogging.Log("EXCEPTION:" & ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
                MQRManager.SendAltertEmail("Shutdown" & ToString(),
                                           "Print Port Listener '" & ToString() & "' has shutdown because of ThreadAbortException and we are meant to be Listening. Why has something told us to abort? " & ControlChars.NewLine & ex.ToString)
            End If
        Catch ex As Exception
            Trace.WriteLine(ToString() & " normal Exception." + ex.Message, TraceLevel.Verbose)
            EventLogging.Log("EXCEPTION:" & ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
            MQRManager.SendAltertEmail("Shutdown" & ToString(), "Print Port Listener '" & ToString() & "' has shutdown because of exception " & ControlChars.NewLine & ex.ToString)
        Finally
            _listening = False

            If Not _server Is Nothing Then
                _server.Stop()
            End If

            Trace.WriteLine(ToString() & " Stopped.", TraceLevel.Verbose)
            NotificationData.SetValue(ToString, ProcessState.Stopped.ToString)

        End Try
    End Function

    Private ReadOnly Property NotificationData() As NotifyData
        Get
            Return NotificationManager.Instance("Processors", "PrinterPortListeners")
        End Get
    End Property

    Public Overrides Function ToString() As String
        Return "PrinterPortListener_" & HostName & "(" & LocalAddr.ToString & "):" & _port.ToString
    End Function

    Private Function DocumentsReceived() As String
        Return _documentsReceived.ToString
    End Function

    '\b[A-Z]\w\d{5}\s\d{2}\/\d{2}\/\d{2}\s\d{2}\.\d{2}\.\d{2}\s\w\d{4}\s+hdr
    Private Function GetResponses(ByVal RawData As String) As ICollection

        Dim Result As New StringDictionary
        Dim Match As Match = Regex.Match(RawData, MQRConfig.Current.MQRServiceConfig.ParseSplitPrintRegEx)

        Do While Match.Success
            Dim NextMatch As Match = Match.NextMatch
            Dim MatchData As String
            If NextMatch.Success Then
                MatchData = RawData.Substring(Match.Index, NextMatch.Index - Match.Index)
            Else
                MatchData = RawData.Substring(Match.Index)
            End If

            Dim PrinterID As String = MatchData.Substring(0, 5)

            If Result.ContainsKey(PrinterID) Then
                Result(PrinterID) += MatchData
            Else
                Result.Add(PrinterID, MatchData)
            End If

            Match = NextMatch
        Loop

        Return Result.Values
    End Function
End Class
