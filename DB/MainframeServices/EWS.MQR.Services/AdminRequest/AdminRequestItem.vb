Imports EWS.Diagnostics
Imports EWS.MQR.Comms
Imports EWS.Network.TCPIP
Imports EWS.MQR.XML
Imports EWS.MQR.Services.MQRManager

Public Class AdminRequestItem

   Public Event SendMessage(ByVal Sender As AdminRequestItem, ByVal args As SendDataEventArgs)

   Private m_ConnectionID As Integer
   Private m_Message As TCPAdminRequest

   Public Sub New(ByVal ConnectionID As Integer, ByVal Message As TCPAdminRequest)
      m_ConnectionID = ConnectionID
      m_Message = Message
   End Sub

   Public ReadOnly Property ConnectionID() As Integer
      Get
         Return m_ConnectionID
      End Get
   End Property

   Public ReadOnly Property Message() As TCPMessage
      Get
         Return m_Message
      End Get
   End Property

   Public Sub Process()


      Dim SendArgs As New SendDataEventArgs(m_ConnectionID, Nothing)

      Try
         Select Case True
            Case m_Message.AdminRequestData.ToUpper = "Status".ToUpper
               Dim ResponseMessage As New TCPAdminResponse(NotificationManager.GetStatus)
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "MQRConfig".ToUpper
               Dim ResponseMessage As New TCPAdminResponse(MQRConfig.Current.ToXML)
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("SaveMQRConfig|")
               Dim ConfigXML As String = Split(m_Message.AdminRequestData, "|")(1)
               MQRConfig.Current.Load(ConfigXML)

               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "GetAllCredentials".ToUpper
               Dim ResponseMessage As New TCPAdminResponse(LogonCredentialManager.GetLogonCredentialsXML)
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("SaveCredential|")
               Dim CredentialXML As String = Split(m_Message.AdminRequestData, "|")(1)
               Dim Credential As New LogonCredential
               Credential.PopulateFromXml(CredentialXML)

               LogonCredentialManager.UpdateCredential(Credential)

               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "GetAllLogonInstructionSets".ToUpper
               Dim ResponseMessage As New TCPAdminResponse(InstructionSetManager.GetAllLogonInstructionSetXML)
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "GetAllParseInstructionSets".ToUpper
               Dim ResponseMessage As New TCPAdminResponse(InstructionSetManager.GetAllParseInstructionsetXML)
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "GetAllQueryInstructionSets".ToUpper
               Dim ResponseMessage As New TCPAdminResponse(InstructionSetManager.GetAllQueryInstructionsetXML)
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "GetAllSessionStates".ToUpper
               Dim ResponseMessage As New TCPAdminResponse(SessionManager.GetSessionStates)
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("GetSessionData|")
               Dim SessionID As String = Split(m_Message.AdminRequestData, "|")(1)
               Dim ResponseMessage As New TCPAdminResponse(SessionManager.GetSessionData(SessionID))
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("MakePoolUnAvailable|")
               Dim PoolName As String = Split(m_Message.AdminRequestData, "|")(1)
               SessionManager.MakePoolUnavailable(PoolName, Date.MaxValue, "Admin Requested Unavailabity")
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("MakePoolAvailable|")
               Dim PoolName As String = Split(m_Message.AdminRequestData, "|")(1)
               SessionManager.MakePoolAvailable(PoolName)
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("KillSession|")
               Dim SessionID As String = Split(m_Message.AdminRequestData, "|")(1)
               SessionManager.QueueSessionShutdown(SessionID, "Kill Session Request from Administrator")
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "KillAllSessions".ToUpper
               SessionManager.CloseAllSessions("Kill All Sessions Request from Administrator")
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "StartSessionStarter".ToUpper
               SessionManager.StartPoolChecker()
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "StopSessionStarter".ToUpper
               SessionManager.StopSessionChecker()
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("ResetSessionCounters|")
               Dim SessionID As String = Split(m_Message.AdminRequestData, "|")(1)
               SessionManager.ResetSessionCountersBySessionID(SessionID)
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("ResetPrinterRetry|")
               Dim SessionID As String = Split(m_Message.AdminRequestData, "|")(1)
               Dim LogonInstructionSet As String = Split(m_Message.AdminRequestData, "|")(2)
               If LogonInstructionSet.ToUpper = "AllSessions".ToUpper Then
                  For Each logonSet As String In InstructionSetManager.GetAllLogonInstructionsetNames
                     With InstructionSetManager.GetLogonInstructionset(logonSet)
                        If SessionID.ToUpper = "All".ToUpper Then
                           .ResetPrinterRetryData()
                        Else
                           .ResetPrinterRetryDataBySessionID(SessionID)
                        End If
                     End With
                  Next
               Else
                  With InstructionSetManager.GetLogonInstructionset(LogonInstructionSet)
                     If SessionID.ToUpper = "All".ToUpper Then
                        .ResetPrinterRetryData()
                     Else
                        .ResetPrinterRetryDataBySessionID(SessionID)
                     End If
                  End With
               End If

               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("MakeSessionUnavailable|")
               Dim SessionID As String = Split(m_Message.AdminRequestData, "|")(1)
               SessionManager.QueueSessionShutdown(SessionID, "Requested By User from Admin Console")
               Dim LogonInstructionSet As String = Split(m_Message.AdminRequestData, "|")(2)
               InstructionSetManager.GetLogonInstructionset(LogonInstructionSet).MakeSessionUnavailable(SessionID, "Requested By User from Admin Console")
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.StartsWith("MakeSessionAvailable|")
               Dim SessionID As String = Split(m_Message.AdminRequestData, "|")(1)
               Dim LogonInstructionSet As String = Split(m_Message.AdminRequestData, "|")(2)
               InstructionSetManager.GetLogonInstructionset(LogonInstructionSet).MakeSessionAvailable(SessionID)
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "ResetAllSessionCounters".ToUpper
               SessionManager.ResetAllSessionCounters()
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "ReloadAllInstructionSets".ToUpper
               InstructionSetManager.LoadAllInstructionSets()
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "ReloadParseInstructionSets".ToUpper
               InstructionSetManager.LoadParseInstructionSets()
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "ReloadLogonInstructionSets".ToUpper
               SessionManager.MakePoolsUnavailable(Date.MaxValue, "Re-loading Logon Instructionsets")
               InstructionSetManager.LoadLogonInstructionSets()
               SessionManager.MakePoolsAvailable()
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "ReloadQueryInstructionSets".ToUpper
               InstructionSetManager.LoadQueryInstructionSets()
               Dim ResponseMessage As New TCPAdminResponse("")
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case m_Message.AdminRequestData.ToUpper = "GetAllQueueData".ToUpper
               Dim ResponseMessage As New TCPAdminResponse(QueueManager.GetQueueData)
               ResponseMessage.RequestID = m_Message.RequestID
               SendArgs.Data = ResponseMessage

            Case Else
               Dim ResponseMessage As New TCPErrorMessage(m_Message.RequestID, "Unreconised Request:" & m_Message.AdminRequestData)
               SendArgs.Data = ResponseMessage

         End Select

      Catch ex As Exception
         EventLogging.Log("EXCEPTION:" & ex.ToString, Me.GetType.Name, EventLogEntryType.Error)
         Dim ResponseMessage As New TCPErrorMessage(m_Message.RequestID, "Exception:" & ex.ToString)
         SendArgs.Data = ResponseMessage
      End Try

      RaiseEvent SendMessage(Me, SendArgs)

   End Sub

End Class
