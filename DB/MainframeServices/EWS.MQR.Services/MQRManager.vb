Imports System.Data.SqlClient
Imports System.Threading
Imports EWS.MQR.XML
Imports EWS.Diagnostics

Public Class MQRManager

#Region " Member Variables "

    Private m_CompleteOutputProcessManager As CompleteOutputQueueProcessorManager
    Private m_ParserProcessManager As ParseQueueProcessorManager
    Private m_RequestQueueProcessManager As RequestQueueProcessorManager
    Private m_CacheWriterQueueProcessManager As CacheWriterProcessorManager
    Private m_CommsManager As New CommsManager

    Private m_ActionLock As New Object

#End Region

#Region " Public Methods "

    Public Sub StartProcesses()

        StartInitalisation()
        EventLogging.Log("Started the MQR Service Processes", Me.GetType.Name, EventLogEntryType.Information)
    End Sub

    Public Sub StopProcesses()

        ShutDown()
        EventLogging.Log("Stopped the MQR Service Processes", Me.GetType.Name, EventLogEntryType.Information)
    End Sub

    Public ReadOnly Property CanRun() As String
        Get
            Dim Result As String = String.Empty

            If InstructionSetManager.Requires3270Sessions Then
                If Not LogonCredentialManager.HaveACredentialToUse Then
                    Result = "No credentials to use"
                End If
            End If

            If Result = String.Empty Then
                If Not CacheWriterProcessorManager.HaveCacheConnectivity Then
                    Result = "Have no cache connectivity"
                End If
            End If

            Return Result
        End Get
    End Property

#End Region

#Region " Contructors "

    Public Sub New()

        AddHandler AppDomain.CurrentDomain.UnhandledException, AddressOf UnhandledException
        FileWriter.Initalise()
    End Sub

#End Region

#Region " Private Methods "

    Private Sub StartInitalisation()
        ThreadManager.Thread("MQRServicesInitalisation", New ThreadManager.DoWork(AddressOf Initalise), ThreadPriority.AboveNormal, False, True)
        ThreadManager.Thread("MQRServicesAdmin", New ThreadManager.DoWork(AddressOf AdminProcess), ThreadPriority.BelowNormal, 500, 30000, True, True)
    End Sub

    Private Sub ShutDown()
        Monitor.Enter(m_ActionLock)
        Try

            m_CommsManager.Shutdown()
            QueueManager.ShutDownQueues()
            ShutDownQueueProcessors()
            SessionManager.ShutDown()
            ShutDownConfiguration()
            ThreadManager.ShutdownThreadStartingWith("File")
            ThreadManager.ShutdownThread("MQRServicesAdmin")

        Catch e As Exception
            EventLogging.Log("ShutDown of MQR Services Failed:" & e.ToString, "MQRManager", EventLogEntryType.Error)
        Finally
            Monitor.Exit(m_ActionLock)
        End Try
    End Sub

    Private Function Initalise() As Boolean

        Dim Success As Boolean = False

        Monitor.Enter(m_ActionLock)
        Try
            InitaliseConfiguration()
            QueueManager.InitaliseQueues()
            InitaliseQueueProcessors()
            SessionManager.InitaliseSessions()
            m_CommsManager.Initialise()
            Success = True
        Catch e As Exception
            EventLogging.Log("Initalisation of MQR Services Failed:" & e.ToString, "MQRManager", EventLogEntryType.Error)
        Finally
            Monitor.Exit(m_ActionLock)
        End Try

        If Not Success Then
            ShutDown()
        End If

        'not used just avoids the warning
        Return True
    End Function

    Private Function AdminProcess() As Boolean
        Try


            If Now.Second = 1 Then
                ErrorResponseLog.Clearup()

                If MQRConfig.Current.MQRServiceConfig.CacheWriteEnabled Then
                    ClenseCache()
                End If


                


                'Check the Services
                For Each ServiceToCheck As String In MQRConfig.Current.MQRServiceConfig.DependentServices.Split(","c)
                    If ServiceToCheck = "" Then
                        Continue For
                    End If
                    
                    Dim ServiceToCheckParts() As String = ServiceToCheck.Split(":"c)
                    If ServiceToCheckParts(0) = "[SNAServer]" Then
                        If (ServiceToCheckParts.Length <= 1)
                            Continue For
                        End If
                        
                        Dim serviceName as String  = ServiceToCheckParts(1)
                        If serviceName = "" Then
                            Continue For
                        End If

                        For Each Host As String In InstructionSetManager.SNAHosts
                            If CheckServiceStatus(Host, serviceName) = ServiceProcess.ServiceControllerStatus.Stopped Then
                                SendAltertEmail(Host & ":" & serviceName & "_ServiceStopped", "The " & Host & ":" & serviceName & " dependent Service is in the Stopped State!")
                            Else
                                EMailAlertPassed(Host & ":" & serviceName & "_ServiceStopped")
                            End If
                        Next
                        
                    Else
                        If CheckServiceStatus(ServiceToCheckParts(0), ServiceToCheckParts(1)) = ServiceProcess.ServiceControllerStatus.Stopped Then
                            SendAltertEmail(ServiceToCheckParts(0) & ":" & ServiceToCheckParts(1) & "_ServiceStopped", "The described dependent Service is in the Stopped State!")
                        Else
                            EMailAlertPassed(ServiceToCheckParts(0) & ":" & ServiceToCheckParts(1) & "_ServiceStopped")
                        End If
                    End If
                Next

            End If

            Return True

        Catch ex As Exception
            EventLogging.Log("Failed Admin Process" & ControlChars.NewLine & ex.ToString, "MQRManager", EventLogEntryType.Error)
            Return False
        End Try
    End Function

    Private Shared Function CheckServiceStatus(Host As String, ServiceName As String) As System.ServiceProcess.ServiceControllerStatus
        Dim Imp As Impersonator = Nothing
        Try


            If Not MQRConfig.Current.MQRServiceConfig.DependentServicesUsername = String.Empty Then
                Imp = New Impersonator(MQRConfig.Current.MQRServiceConfig.DependentServicesUsername, MQRConfig.Current.MQRServiceConfig.DependentServicesPassword)
            End If


            For Each Service As System.ServiceProcess.ServiceController In System.ServiceProcess.ServiceController.GetServices(Host)
                If Service.DisplayName = ServiceName Then
                    If Service.Status = ServiceProcess.ServiceControllerStatus.Stopped And MQRConfig.Current.MQRServiceConfig.RestartDependentServices Then
                        Try
                            EventLogging.Log("Restarting Service:" & Host & ":" & ServiceName & " as it was stopped!", "MQRManager", EventLogEntryType.Error)
                            Service.Start()
                            Service.WaitForStatus(ServiceProcess.ServiceControllerStatus.Running, New TimeSpan(0, 1, 0))
                        Catch ex As Exception
                            EventLogging.Log("Exception trying to start service:" & Host & ":" & ServiceName & ControlChars.NewLine & ex.ToString, "MQRManager", EventLogEntryType.Error)
                        End Try
                    End If
                    Return Service.Status
                End If
            Next

            Return ServiceProcess.ServiceControllerStatus.Stopped
        Catch ex As Exception
            EventLogging.Log("Exception retriveing service satus:" & Host & ":" & ServiceName & ControlChars.NewLine & ex.ToString, "MQRManager", EventLogEntryType.Error)
            Return ServiceProcess.ServiceControllerStatus.Stopped
        Finally
            If Not Imp Is Nothing Then
                Imp.Dispose()
                Imp = Nothing
            End If
        End Try
    End Function

    Private Shared Sub InitaliseConfiguration()
        LogonCredentialManager.Initalise(AddressOf EmailSender.SendEMailMessage)
        InstructionSetManager.LoadAllInstructionSets()
    End Sub

    Public Shared Sub EMailAlertPassed(Alert As String)
        If m_LastMailSentUTC.ContainsKey(Alert) Then
            m_LastMailSentUTC(Alert) = Date.MinValue
        Else
            m_LastMailSentUTC.Add(Alert, Date.MinValue)
        End If
    End Sub

    Private Shared m_LastMailSentUTC As New Dictionary(Of String, Date)

    Public Shared Sub SendAltertEmail(Alert As String, ByVal ReasonText As String)

        Try

            Dim SendMessage As Boolean = False

            If Not m_LastMailSentUTC.ContainsKey(Alert) Then
                SendMessage = True
                m_LastMailSentUTC.Add(Alert, Date.UtcNow)
            Else
                If m_LastMailSentUTC(Alert).AddMinutes(MQRConfig.Current.MQRServiceConfig.AlertFrequencyMins) < Date.UtcNow Then
                    SendMessage = True
                End If
            End If

            If SendMessage Then

                EmailSender.SendEMailMessage(MQRConfig.Current.MQRServiceConfig.GeneralEMailAltertTo, "MQR " & Alert & "Alert", "MQR has raised an alert because :" & ReasonText & ", please check the state of the service!", True)
                m_LastMailSentUTC(Alert) = Date.UtcNow

                EventLogging.Log(Alert & ControlChars.NewLine & ReasonText, "MQRManager", EventLogEntryType.Warning)

            End If
        Catch ex As Exception
            EventLogging.Log("Failed Sending Alter EMail for " & Alert & ControlChars.NewLine & ReasonText & ControlChars.NewLine & ex.ToString, "MQRManager", EventLogEntryType.Warning)
        End Try
    End Sub


    Private Sub InitaliseQueueProcessors()

        m_ParserProcessManager = New ParseQueueProcessorManager
        m_ParserProcessManager.StartProcesses()

        If InstructionSetManager.Requires3270Sessions Then
            m_CompleteOutputProcessManager = New CompleteOutputQueueProcessorManager
            m_CompleteOutputProcessManager.StartProcesses()

            m_CacheWriterQueueProcessManager = New CacheWriterProcessorManager
            m_CacheWriterQueueProcessManager.StartProcesses()

        End If

        m_RequestQueueProcessManager = New RequestQueueProcessorManager
        m_RequestQueueProcessManager.StartProcesses()
    End Sub

    Private Sub ShutDownQueueProcessors()

        If Not m_RequestQueueProcessManager Is Nothing Then
            m_RequestQueueProcessManager.StopProcesses()
            m_RequestQueueProcessManager = Nothing
        End If

        If Not m_ParserProcessManager Is Nothing Then
            m_ParserProcessManager.StopProcesses()
            m_ParserProcessManager = Nothing
        End If

        If Not m_CacheWriterQueueProcessManager Is Nothing Then
            m_CacheWriterQueueProcessManager.StopProcesses()
            m_CacheWriterQueueProcessManager = Nothing
        End If

        If Not m_CompleteOutputProcessManager Is Nothing Then
            m_CompleteOutputProcessManager.StopProcesses()
            m_CompleteOutputProcessManager = Nothing
        End If
    End Sub

    Private Sub ClenseCache()
        Try
            Dim Connection As New SqlConnection(MQRConfig.Current.MQRServiceConfig.CacheConnectionString)
            Dim Command As New SqlCommand()
            With Command
                .Connection = Connection
                .CommandType = CommandType.StoredProcedure
                .CommandText = "SP_DataClearup"
                .CommandTimeout = 60
            End With

            Try

                With Connection
                    .Open()
                    With Command
                        Try
                            .ExecuteNonQuery()
                        Catch ex As Exception
                            Trace.WriteLine(String.Format("DataClearup - {0}", ex.Message), TraceLevel.Error)
                        Finally
                            .Dispose()
                        End Try
                    End With
                End With
            Catch ex As Exception
                EventLogging.Log(MQRConfig.Current.MQRServiceConfig.CacheConnectionString & ControlChars.NewLine & ex.ToString, "MQRCache", EventLogEntryType.Error)
            Finally
                With Connection
                    .Close()
                    .Dispose()
                End With
            End Try

        Catch ex As Exception
            EventLogging.Log(ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
        End Try
    End Sub

    Private Shared Sub ShutDownConfiguration()
        InstructionSetManager.ClearAllinstructionSets()
        LogonCredentialManager.ShutDown()
    End Sub


#End Region

#Region " Event Handlers "

    Private Sub UnhandledException(ByVal sender As Object, ByVal e As System.UnhandledExceptionEventArgs)
        Try
            If e.IsTerminating Then
                EventLogging.Log("UNHANDLED EXCEPTION! - " & e.ExceptionObject.ToString, Me.GetType.Name, EventLogEntryType.Error)
            Else
                EventLogging.Log("UNHANDLED EXCEPTION! - " & e.ExceptionObject.ToString, Me.GetType.Name, EventLogEntryType.Warning)
            End If
        Catch ex As Exception
            Trace.WriteLine("UNHANDLED EXCEPTION, Excepted! - Terminating: " & e.IsTerminating.ToString & " " & e.ExceptionObject.ToString, TraceLevel.Error)
        End Try
    End Sub

#End Region
End Class
