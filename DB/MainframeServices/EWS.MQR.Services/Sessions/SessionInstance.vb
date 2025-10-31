Imports System.Threading
Imports System.String
Imports EWS.MQR.XML
Imports System.Configuration
Imports System.Collections.Specialized
Imports EWS.MQR.XML.ScreenInput
Imports System.Text
Imports EWS.Diagnostics

Public Class SessionInstance

   Implements Open3270.IAudit

#Region " Member Variables "

   Public Const NOTIFY_CATEGORY As String = "3270Sessions"

   Private m_Emulator As Open3270.TNEmulator
   Private m_HomeScreen As ScreenIdentificationMarks
   Private m_CollectedScreenTrace As New StringBuilder
   Private m_IsLocked As Boolean = False
   Private m_LockedTimeOut As Date = Date.MinValue
   Private m_State As SessionStateKind = SessionStateKind.Inital
   Private m_LastStateChangeUTC As Date = Date.UtcNow
   Private m_QueriesRun As Long
   Private m_ProcessingQuery As String = "NONE"
   Private m_CurrentQueryRequestTimeOutSeconds As Integer = 0
   Private m_SessionID As String
   Private m_Priority As Integer
   Private m_Credential As LogonCredential
   Private m_LogonInstructionSet As LogonInstructionSet
   Private m_LogonCapturedData As New Dictionary(Of String, String)
   Private m_QueryCapturedData As New Dictionary(Of String, String)
   Private m_LastScreenSet As String
   Private m_LastResponse As String
   Private m_EmulatorAccessor As New Object
   Private m_InitalisationThread As Thread

#End Region

#Region " Constructors "

   Public Sub New(ByVal InstructionSet As LogonInstructionSet, ByVal InstructionSetSession As LogonSession)

      LockSession("Initalisation")
      m_LogonInstructionSet = InstructionSet
      m_SessionID = InstructionSetSession.SessionID
      m_Priority = InstructionSetSession.Priority
      m_Credential = LogonCredentialManager.GetCredential(InstructionSet.CredentialPool)
      If m_Credential Is Nothing Then
         Throw New NoFreeCredentialsException("Could not obtain Logon Credential for " & InstructionSet.Identifier & ".")
      Else
         Trace.WriteLine("Using Credential " & m_Credential.Identifier & " for session " & m_SessionID, TraceLevel.Verbose)
      End If

      NotificationData.SetDelegate(m_SessionID, New GetNotifyDataValue(AddressOf Status))

      m_InitalisationThread = New Thread(AddressOf InitaliseEmulator)
      With m_InitalisationThread
         .Priority = ThreadPriority.AboveNormal
         .Name = "SessionInitaliser:" & m_SessionID
         .Start()
      End With

      'ThreadManager.Thread("SessionInitaliser:" & m_SessionID, New ThreadManager.DoWork(AddressOf InitaliseEmulator), ThreadPriority.Normal, True, True)

   End Sub

#End Region

#Region " Public Methods "

   Public ReadOnly Property LogonInstructionSet As LogonInstructionSet
      Get
         Return m_LogonInstructionSet
      End Get
   End Property

   Public ReadOnly Property CollectedDataFromQuery() As Dictionary(Of String, String)
      Get
         Return m_QueryCapturedData
      End Get
   End Property

   Public ReadOnly Property SessionID() As String
      Get
         Return m_SessionID
      End Get
   End Property

   Public ReadOnly Property IsLocked() As Boolean
      Get
         Return m_IsLocked
      End Get
   End Property

   Public ReadOnly Property Priority() As Integer
      Get
         Return m_Priority
      End Get
   End Property

   Public ReadOnly Property LastUsedUTC() As Date
      Get
         Return m_LastStateChangeUTC
      End Get
   End Property

   Public Sub ReleaseSeeionUseFromLogonInstructionSet()
      m_LogonInstructionSet.Sessions(m_SessionID).ReleaseUse()
   End Sub


   Public Const TIME_OUT_RESPONSE As String = "Timed Out Item"

   Friend Function ReceivedResponse(ByVal Response As String) As Dictionary(Of String, String)

      m_LastResponse = Response


      If Not Response = TIME_OUT_RESPONSE Then

         Trace.WriteLine("Received Response: " & ToString(), TraceLevel.Verbose)

         Dim SleepCount As Integer

         Do While Not State() = SessionStateKind.AwaitingResponse
            If SleepCount < 100 Then
               SleepCount += 1
               System.Threading.Thread.Sleep(100)
            Else
               Throw New SessionReleaseExecption("Session was in the wrong state to release it after waiting 10 secs! State was:" & State.ToString)
            End If
         Loop
         SetState(SessionStateKind.ReadyForCommand)
         UnLockSession("Received a good response from printer")
      Else
         SetState(SessionStateKind.ReadyForCommand)
         UnLockSession("Item Timed Out")
      End If

      Return New Dictionary(Of String, String)(m_QueryCapturedData)
   End Function

   Friend Sub LockSession(ByVal Reason As String)

      If Not m_IsLocked Then
         m_LockedTimeOut = Date.UtcNow.AddMinutes(MQRConfig.Current.MQRServiceConfig.SessionStuckTimeoutMinutes)
         m_IsLocked = True
      End If

      Trace.WriteLine("Locked Session: " & ToString() & " to " & Reason, TraceLevel.Verbose)
   End Sub

   Friend Sub UnLockSession(ByVal Reason As String)
      If m_IsLocked Then
         m_LockedTimeOut = Date.MinValue
         m_IsLocked = False
      End If

      m_ProcessingQuery = "NONE"
      m_CurrentQueryRequestTimeOutSeconds = 0

      Trace.WriteLine("Unlocked Session: " & ToString() & " because " & Reason, TraceLevel.Verbose)
   End Sub

   Public Function IsHealthy(ByRef Reason As String) As Boolean

      Reason = String.Empty

      'only check if we have been sat at this state for a period of time that would not consistute in the process of chanaging
      If m_LastStateChangeUTC.AddSeconds(30) < Date.UtcNow Then

         Select Case State()

            Case SessionStateKind.AwaitingResponse, SessionStateKind.ReadyForCommand

               If m_Emulator Is Nothing Then
                  Reason = "Emulator not intialised"
               Else
                  If m_Emulator.IsConnected Then
                     If IsLocked Then
                        If m_LockedTimeOut > Date.UtcNow Then
                           Reason = String.Empty
                        Else
                           Reason = "Loked and, been Locked for " & MQRConfig.Current.MQRServiceConfig.SessionStuckTimeoutMinutes & " in State of " & State.ToString & "."
                        End If
                     Else

                        Dim FailureMark As String = String.Empty
                        If IsCorrectScreen(m_HomeScreen, FailureMark, True) Then
                           Reason = String.Empty
                        Else
                           Reason = "Home Screen Incorrect - " & FailureMark & ControlChars.NewLine & VisibleScreen()
                        End If

                     End If
                  Else
                     Reason = "Emulator Not Connected"
                  End If
               End If


            Case SessionStateKind.CheckingAvailability, _
                 SessionStateKind.Inital, _
                 SessionStateKind.LoggingOn

               If m_LastStateChangeUTC.AddMinutes(5) < Date.UtcNow Then
                  Reason = "Emulator in state " & State().ToString & " for " & (Date.UtcNow - m_LastStateChangeUTC).TotalSeconds & " seconds."
               Else
                  Reason = String.Empty
               End If

            Case SessionStateKind.Processing, SessionStateKind.ShutDownInProgress
               Reason = String.Empty

            Case Else
               Throw New InvalidConstraintException("State unreconised : " & State.ToString)

         End Select
      End If
      Return (Reason = String.Empty)
   End Function

   Public Sub DoQuery(ByVal Request As RequestItem, ByRef WaitForResponse As Boolean, ByRef NoResponseText As String)

      Dim ShutDownReason As String = String.Empty

      Try
         QueueManager.EmptyQueuesForSession(TOPSAltName, "about to start a new query")
         m_LastResponse = ""
         m_QueryCapturedData.Clear()

         If Not IsCorrectScreen(m_HomeScreen, ShutDownReason, True) Then
            ShutDownReason = "Incorrect Screen before the query ran:" & ShutDownReason
         Else
            WaitForResponse = True

            Dim GetValueMethod As New GetVariableValueDelegate(AddressOf Request.GetFieldValue)

            If Not State() = SessionStateKind.ReadyForCommand Then
               Throw New SessionNotReadyException("Session: " & m_SessionID & " state is currently " & m_State.ToString & ". Cannot run query!")
            End If

            Monitor.Enter(m_EmulatorAccessor)

            Try

               SetState(SessionStateKind.Processing)

               Request.QuerySubmitted(TOPSAltName)

               m_ProcessingQuery = Request.QueryIdentifier & " - Parameters:" & Request.ParametersString.Replace(" ", "")
               m_CurrentQueryRequestTimeOutSeconds = Request.Request.TimeOutSeconds

               m_QueriesRun += 1

               With Request.QueryInstructionSetToUse

                  Dim MFTrans As New MFTransaction.TransactionData(Request, m_Credential.UserName, m_SessionID)

                  WriteInput(.Inputs, GetValueMethod)
                  DoNavigation(MFTrans, .NavigationAction, Request.QueryIdentifier & "QueryNavigationAction")
                  DoProcessActions(MFTrans, .Actions, Request.QueryIdentifier & "QueryProcessActions", WaitForResponse, ShutDownReason, GetValueMethod)

                  If Not ShutDownReason = String.Empty Then
                     WaitForResponse = False
                     NoResponseText = ShutDownReason
                  Else
                     If .SuccessNoDataCondition.Count > 0 AndAlso CheckSuccessConditions(.SuccessNoDataCondition, False) Then
                        WaitForResponse = False
                        NoResponseText = "Success NoData Condition Met"
                     Else
                        If CheckSuccessConditions(.SuccessCondition, True) Then
                           If Not WaitForResponse Then
                              NoResponseText = "Process Actions denoted that No response would be recieved yet it was a success ???"
                              If Debugger.IsAttached Then
                                 Debugger.Break()
                              End If
                           End If
                        Else
                           WaitForResponse = False
                           ShutDownReason = "Failed Success Condition Check."
                           NoResponseText = "Failed Success Condition Check."
                        End If
                     End If
                  End If
               End With

            Catch e As Exception
               ShutDownReason = "Failed Running Query '" + Request.QueryIdentifier + "."
               WaitForResponse = False

               TraceToCollectedScreen("", False, False)
               TraceToCollectedScreen("", False, True)
               TraceToCollectedScreen(FormatException("Failed Running Query:", e), True, False)
               TraceToCollectedScreen("", False, True)
               TraceToCollectedScreen("", False, False)
            Finally
               Monitor.Exit(m_EmulatorAccessor)
            End Try


         End If

         If ShutDownReason = String.Empty Then
            Trace.WriteLine("3270Session:" & m_SessionID & " Credential: " & m_Credential.Identifier & " Completed Running Query: " & m_ProcessingQuery & " from InstructionSet:" & m_LogonInstructionSet.Identifier, TraceLevel.Verbose)
            If WaitForResponse Then
               SetState(SessionStateKind.AwaitingResponse)
            Else
               SetState(SessionStateKind.ReadyForCommand)
               UnLockSession("Not waiting for response")
            End If
         Else
            Trace.WriteLine("3270Session:" & m_SessionID & " Credential: " & m_Credential.Identifier & " Failed Running Query: " & m_ProcessingQuery & " from InstructionSet:" & m_LogonInstructionSet.Identifier, TraceLevel.Verbose)

            'OK Something has gone pete tong and we could have started a print so:
            'wait a bit for and possible prints to come through
            System.Threading.Thread.Sleep(5000)

            'Now clear them
            QueueManager.EmptyQueuesForSession(TOPSAltName, "errored while doing a query")
            ShutdownClean(False, "Do Query Requested Shutdown - " & ShutDownReason)
            Throw New QueryFailureException("FailureReason: " & ShutDownReason & " Query: " & Request.QueryIdentifier)
         End If


      Finally
         Write3270ScreenDumpToFile("Run Query - " & m_ProcessingQuery & " Success : " & (ShutDownReason = String.Empty).ToString, Request.QueryIdentifier, Request.ParametersString)
      End Try

   End Sub

   Public Sub QueuedShutdown(ByVal Reason As String)
      TraceToCollectedScreen("Queued ShutDown : " & Reason, True, True)
      LockSession("ShutDown")
      SetState(SessionStateKind.ShutDownInProgress)
   End Sub

   Public Sub TerminateSession(ByVal Reason As String)

      Try
         Try
            EWS.Diagnostics.NotificationManager.RemoveItem(NOTIFY_CATEGORY, m_LogonInstructionSet.Identifier, m_SessionID)
         Catch e As Exception
            LogToEventLog(e.ToString, EventLogEntryType.Error)
         End Try

         TraceToCollectedScreen("Terminate : " & Reason, True, True)

         Try
            If Not m_InitalisationThread Is Nothing Then
               If Not m_InitalisationThread.Join(5000) Then
                  'force the sate to shutdown and abort              
                  m_InitalisationThread.Abort()
               End If
            End If

            If Not m_Emulator Is Nothing Then
               With m_Emulator
                  .Debug = False
                  .Audit = Nothing
                  .Close()
               End With
            End If
         Catch e As Exception
            LogToEventLog("Exception shutting down - " & e.ToString, EventLogEntryType.Error)
         Finally
            m_Emulator = Nothing
         End Try
      Finally
         Write3270ScreenDumpToFile("Terminate Session Complete", "Teminate", m_SessionID)
      End Try

   End Sub

   Public ReadOnly Property TOPSName() As String
      Get
         If m_LogonCapturedData.ContainsKey("TOPSName") Then
            Return m_LogonCapturedData("TOPSName")
         Else
            Return "UNKNOWN"
         End If
      End Get
   End Property

   Public Function PrinterRetryData() As String

      Return m_LogonInstructionSet.GetPrinterRetryData(m_SessionID)

   End Function

   Public ReadOnly Property CurrentPrinterState() As String
      Get
         If m_LogonCapturedData.ContainsKey("PrinterState") Then
            Return m_LogonCapturedData("PrinterState")
         Else
            Return "UNKNOWN"
         End If
      End Get
   End Property

   Public ReadOnly Property CredentialName() As String
      Get
         If m_Credential Is Nothing Then
            Return "UNKNOWN"
         Else
            Return m_Credential.UserName
         End If
      End Get
   End Property

   Public ReadOnly Property TOPSAltName() As String
      Get
         If m_LogonCapturedData.ContainsKey("TOPSAltName") Then
            Return m_LogonCapturedData("TOPSAltName")
         Else
            Return "UNKNOWN"
         End If
      End Get
   End Property

   Public ReadOnly Property PoolName() As String
      Get
         Return m_LogonInstructionSet.Identifier
      End Get
   End Property

   Public Function IsTOPSName(ByVal Name As String) As Boolean
      Dim Result As Boolean
      Select Case True
         Case (String.Compare(Name, TOPSName, True) = 0)
            Result = True
            Trace.WriteLine("Found Match on TOPSName " & TOPSName & ":" & Name, TraceLevel.Verbose)

         Case (String.Compare(Name, TOPSAltName, True) = 0)
            Result = True
            Trace.WriteLine("Found Match on TOPSAltName " & TOPSName & ":" & Name, TraceLevel.Verbose)

         Case Else
            Result = False
      End Select

      Return Result
   End Function

   Public Function GetScreenTrace(ByVal IncludeCurrent As Boolean) As String
      If IncludeCurrent Then
         TraceToCollectedScreen("CURRENT SCREEN", False, True)
         TraceToCollectedScreen(VisibleScreen, False, False)
      End If

      Return m_CollectedScreenTrace.ToString
   End Function

   Public Function LastScreenSet() As String
      Return m_LastScreenSet
   End Function

   Public Function VisibleScreen() As String
      Try
         If Not m_Emulator Is Nothing Then
                If m_Emulator.IsConnected AndAlso Not m_Emulator.CurrentScreenXML Is Nothing AndAlso Not m_Emulator.CurrentScreenXML.Dump Is Nothing Then
                    Return m_Emulator.CurrentScreenXML.Dump
                Else
                    Return "-- NOT CONNECTED --"
            End If
         Else
            Return "-- NO EMULATOR --"
         End If
      Catch ex As Exception
         Trace.WriteLine("Excepted while trying to retrive Screen :" & ex.ToString, TraceLevel.Error)
         Return "-- EXCEPTED WHILEST TRYING TO ACCESS SCREEN --"
      End Try
   End Function

   Public Function QueriesRun() As Long
      Return m_QueriesRun
   End Function

   Public Function Elapsed() As String

      Dim TS As New TimeSpan(Date.UtcNow.Ticks - m_LastStateChangeUTC.Ticks)
      Return String.Format("{0}D {1}:{2}:{3}", TS.Days.ToString("000"), TS.Hours.ToString("00"), TS.Minutes.ToString("00"), TS.Seconds.ToString("00"))

   End Function

   Public Function LockedTimeout() As String

      If Not m_IsLocked Then
         Return "Not Locked"
      Else
         If m_LockedTimeOut = Date.MinValue Then
            Return "Locked but no TimeoutSet!"
         Else
            Dim TS As New TimeSpan(m_LockedTimeOut.Ticks - Date.UtcNow.Ticks)
            Return String.Format("{0}:{1}", TS.TotalMinutes.ToString("00"), TS.Seconds.ToString("00"))
         End If
      End If

   End Function

   Public Function State() As SessionStateKind
      Return m_State
   End Function

   Public Function GetStateData() As SessionStateDataItem
      Dim Result As New SessionStateDataItem

      With Result
         .Credential = m_Credential.Identifier
         .CurrentQuery = m_ProcessingQuery
         .CurrentQueryRequestTimeOut = m_CurrentQueryRequestTimeOutSeconds
         .Elapsed = Elapsed()
         .LockedTimeout = LockedTimeout()
         .IsLocked = m_IsLocked
         .Pool = PoolName
         .QueriesComplete = m_QueriesRun
         .SessionID = m_SessionID
         .State = m_State.ToString
         .TOPSAltName = TOPSAltName
         .TOPSName = TOPSName
         .Priority = m_Priority
         .PrinterState = PrinterRetryData()
         .MFTimeSync = SessionManager.ServerToMFTimeDiff.ToString
         .TimeOutItems = m_LogonInstructionSet.Sessions(m_SessionID).TimeOutCount
         .Available = m_LogonInstructionSet.Sessions(m_SessionID).Available
      End With

      Return Result
   End Function

   Public Function GetSessionData() As SessionData

      Dim Data As New SessionData

      Data.SessionState = GetStateData()
      Data.CurrentScreen = VisibleScreen()
      Data.LastTransactionScreen = LastScreenSet()
      Data.LastResponse = LastResponse

      Return Data

   End Function

   Public Sub ResetCounters()
      m_QueriesRun = 0
      m_LastStateChangeUTC = Date.UtcNow
   End Sub

   Public ReadOnly Property LastResponse() As String
      Get
         Return m_LastResponse
      End Get
   End Property

   Public Sub TimedOut()
      m_LogonInstructionSet.SessionTimeOut(m_SessionID)
   End Sub

#End Region

#Region " Private Methods "

   Private Sub ShutdownClean(ByVal DueToErrorCondition As Boolean, ByVal Reason As String)

      Dim Msg As String = "Session Is being ShutDown because: " & Reason

      If DueToErrorCondition Then
         LogToEventLog(Msg, EventLogEntryType.Error)
      Else
         Trace.WriteLine(Msg, TraceLevel.Verbose)
      End If

      SessionManager.QueueSessionShutdown(m_SessionID, Reason)

   End Sub

   Private Sub CalculateServerToMFTimeShift(ByVal ServerTime As Date, ByRef ShutdownReason As String)

      If m_LogonInstructionSet.LogonInstructions.MFDateTimeScreenCapturePoints.Count = 2 Then

         TraceScreenToCollectedScreen()
         ExtractCapturePoints(m_LogonInstructionSet.LogonInstructions.MFDateTimeScreenCapturePoints)

         If (m_LogonCapturedData.ContainsKey("MFDate") And m_LogonCapturedData.ContainsKey("MFTime")) Then

            Dim MFDate As String = m_LogonCapturedData("MFDate").Trim
            Dim MFTime As String = m_LogonCapturedData("MFTime").Trim

            Try
               Dim MFDateTime As Date = Date.ParseExact(MFDate & " " & MFTime, "dd/MM/yy HH:mm:ss", System.Globalization.DateTimeFormatInfo.InvariantInfo)
               SessionManager.UpdateTimeShift(ServerTime, MFDateTime)
               TraceToCollectedScreen("Server Time : " & ServerTime.ToString("HH:mm:ss"), False, True)
               TraceToCollectedScreen("MF     Time : " & MFDateTime.ToString("HH:mm:ss"), False, True)
               TraceToCollectedScreen("Server -> MF Time shift: " & SessionManager.ServerToMFTimeDiff.ToString, False, True)
               TraceToCollectedScreen("", False, True)
            Catch ex As Exception
               ShutdownReason = "could not calculate TimeSpan between MFTime:" & MFDate & " " & MFTime & " ServerTime: " & ServerTime.ToString & ControlChars.NewLine & ex.ToString
            End Try
         Else
            ShutdownReason = "could not extract MainFrame date from instructionset " & m_LogonInstructionSet.Identifier
         End If
      Else
         ShutdownReason = "there were insufficent MFDateTimeScreenCapturePoints in instructionset " & m_LogonInstructionSet.Identifier
      End If
   End Sub

   Private Sub DoLogon(ByVal ServerTime As Date, ByRef ShutdownReason As String)

      Dim MFTrans As New MFTransaction.TransactionData("MQR", "Logon", Guid.NewGuid, m_LogonInstructionSet.Identifier, m_Credential.UserName, m_SessionID)

      Dim SuccessfulLogon As Boolean = True

      Monitor.Enter(m_Credential.UseLock)
      Try

         Trace.WriteLine("3270Session:" & m_SessionID & " Credential: " & m_Credential.Identifier & " Is LoggingOn", TraceLevel.Verbose)

         SetState(SessionStateKind.LoggingOn)

         With m_LogonInstructionSet.LogonInstructions
            If WaitForScreens(.InitalScreenIndetificationMarkers, m_LogonInstructionSet.Connection.ConnectionTimeOut) Then

               CalculateServerToMFTimeShift(ServerTime, ShutdownReason)

               If Not ShutdownReason = String.Empty Then
                  SuccessfulLogon = False
               Else

                  Dim FailureMark As String = String.Empty
                  If IsCorrectScreen(.InitalScreenIndetificationMarkers, FailureMark, True) Then
                     WriteInput(.Inputs, New GetVariableValueDelegate(AddressOf GetUserNamePasswordFromCredentials))
                     If DoPasswordChange(.PasswordChange, .NavigationAction, ShutdownReason, MFTrans) Then

                        DoProcessActions(MFTrans, .Actions, "Logon", False, ShutdownReason, Nothing)

                        If Not ShutdownReason = String.Empty Then
                           SuccessfulLogon = False
                        Else
                           If TOPSName = String.Empty And TOPSAltName = String.Empty Then
                              ShutdownReason = "Could not capture TOPSName."
                              SuccessfulLogon = False
                           Else

                              If CurrentPrinterState = .ResetPrinterProcess.ActivePrinterState Then
                                 SuccessfulLogon = CheckSuccessCondition(.SuccessCondition, True)
                                 If Not SuccessfulLogon Then
                                    ShutdownReason = "Failure of Success Condition."
                                 End If
                              Else
                                 Dim PrinterState As String = CurrentPrinterState
                                 Dim FailReason As String = String.Empty

                                 TraceToCollectedScreen("", False, True)
                                 TraceToCollectedScreen("RESETTING PRINTER STATUS WAS " & CurrentPrinterState, False, True)
                                 TraceToCollectedScreen("RESETTING PRINTER STATUS SHOULD BE " & .ResetPrinterProcess.ActivePrinterState, False, True)
                                 TraceToCollectedScreen("", False, True)

                                 SuccessfulLogon = DoPrinterReset(m_LogonInstructionSet.LogonInstructions.ResetPrinterProcess, FailReason, MFTrans)
                                 If SuccessfulLogon Then
                                    SuccessfulLogon = CheckSuccessCondition(.SuccessCondition, True)
                                    If Not SuccessfulLogon Then
                                       ShutdownReason = "Failure of Success Condition after printer reset."
                                    End If
                                 Else
                                    ShutdownReason = "Printer State is invalid State was '" & PrinterState & "' have tried to reset back to " & .ResetPrinterProcess.ActivePrinterState & " but could not because " & FailReason & ControlChars.NewLine & GetScreenTrace(True)
                                 End If
                              End If
                           End If
                        End If
                     Else
                        ShutdownReason = "Password Change Failed!"
                        SuccessfulLogon = False
                        m_Credential.SetPasswordChangedDateDontSave(Now)
                        m_Credential.LockOut()
                     End If
                  Else
                     ShutdownReason = "Could not Identify Inital Screen: " & FailureMark
                     SuccessfulLogon = False
                  End If
               End If
            Else
               ShutdownReason = "Screen did not Initalise: " & .InitalScreenIndetificationMarkers(0).Identifier
               SuccessfulLogon = False
            End If

         End With
      Finally
         Monitor.Exit(m_Credential.UseLock)
      End Try

      If SuccessfulLogon Then
         Trace.WriteLine(ToString() & " Is loggedOn Successfully", TraceLevel.Verbose)
         UnLockSession("Successful Logon")
         m_HomeScreen = m_LogonInstructionSet.LogonInstructions.SuccessCondition.IdentifactionMarks
         SetState(SessionStateKind.ReadyForCommand)
      Else
         Throw New SessionInitalisationException("Failed to logon because " & ShutdownReason)
      End If

   End Sub

   Private Function DoPrinterReset(ByVal Process As ResetPrinterProcess, ByRef FailureReason As String, ByRef MFTrans As MFTransaction.TransactionData) As Boolean

      Dim Success As Boolean = False

      Dim CompareDate As Date = m_LogonInstructionSet.LastPrinterReset(m_SessionID, Process.AttemptsBeforeFailure)

      If CompareDate.AddMinutes(Process.ResetPeriodMins) < Date.UtcNow Then

         Success = ResetPrinterState(Process, FailureReason, MFTrans)

         If Success Then
            m_LogonInstructionSet.UpdatePrinterResetData(m_SessionID, Process.AttemptsBeforeFailure, CurrentPrinterState)
         Else
            FailureReason = "Failed to reset Status back to active status '" & Process.ActivePrinterState & "' because '" & FailureReason & "'."
         End If
      Else
         FailureReason = "Have completed more than " & Process.AttemptsBeforeFailure & " resets in the last " & Process.ResetPeriodMins & " minutes : " & PrinterRetryData()
      End If

      If Not Success Then
         m_LogonInstructionSet.MakeSessionUnavailable(m_SessionID, "Invalid Printer State '" & CurrentPrinterState & "' " & FailureReason)
      End If

      Return Success
   End Function

   Private Function ResetPrinterState(ByVal Process As ResetPrinterProcess, ByRef FailureReason As String, ByRef MFTrans As MFTransaction.TransactionData) As Boolean

      Dim Result As Boolean = True

      Dim MSG As String = "3270Session:" & m_SessionID & " Credential: " & m_Credential.Identifier & " is reseting printer state to '" & Process.ActivePrinterState & "'. Current State is " & CurrentPrinterState

      Trace.WriteLine(MSG, TraceLevel.Verbose)
      FileWriter.Write("3270PrinterResetLog", MSG, Nothing, TOPSAltName)

      For Each Action As ResetPrinterAction In Process.Actions

         Select Case Action.Type
            Case PrinterActionType.Navigation
               SendKeyCommand(MFTrans, DirectCast([Enum].Parse(GetType(KeyCommand), Action.Value, True), KeyCommand), Process.NavigationTimeout, "ResetPrinter", 1)
               TraceScreenToCollectedScreen()

            Case PrinterActionType.SendText
               If IsNumeric(Action.AdditionalInfo) Then
                  m_Emulator.SetField(CInt(Action.AdditionalInfo), Action.Value)
               Else
                  m_Emulator.SendText(Action.Value)
               End If
               TraceToCollectedScreen("Writing text to screen:" & Action.Value, True, True)
               TraceScreenToCollectedScreen()

            Case PrinterActionType.WaitForRegExMatch

               Dim Parts() As String = Action.AdditionalInfo.Split(","c)

               Dim Timeout As Integer = CInt(Parts(0))
               Dim Options As RegularExpressions.RegexOptions = DirectCast([Enum].Parse(GetType(RegularExpressions.RegexOptions), Parts(1), True), RegularExpressions.RegexOptions)
               Dim StartRow As Integer = CInt(Parts(2))
               Dim EndRow As Integer = CInt(Parts(3))
               Dim StartCol As Integer = CInt(Parts(4))
               Dim EndCol As Integer = CInt(Parts(5))

               Try
                  Result = m_Emulator.WaitForText(StartRow, EndRow, StartCol, EndCol, Action.Value, Options, Timeout)
                  If Not Result Then
                     FailureReason = "Screen did not refresh to the correct state regex """ & Action.Value & ""
                  End If
               Catch ex As Exception
                  Result = False
                  FailureReason = "Screen did not refresh to the correct state regex """ & Action.Value & " becuase " & ex.ToString()
               End Try

               If Not FailureReason = String.Empty Then
                  TraceToCollectedScreen(FailureReason, False, True)
               End If

               TraceScreenToCollectedScreen()

         End Select

         If Not Result Then Exit For

      Next

      m_LogonCapturedData("PrinterState") = Process.ActivePrinterState

      'CheckAvailabilty it has been set.

      FileWriter.Write("3270PrinterResetLog", GetScreenTrace(True))
      FileWriter.Write("3270PrinterResetLog", "Reste Complete Result: " & Result)

      Return Result

   End Function

   Private Function FormatException(ByVal Message As String, Optional ByVal ex As Exception = Nothing) As String
      Dim result As New StringBuilder

      result.AppendLine(Message)
      result.AppendLine("")

      If Not ex Is Nothing Then
         result.AppendLine("EXCEPTION:")
         result.AppendLine(ex.ToString)
         result.AppendLine("")
      End If

      result.AppendLine("SCREEN:")
      result.AppendLine(GetScreenTrace(True))

      Return result.ToString

   End Function

   Private Sub CheckAvailabilty()
      Trace.WriteLine("3270Session:" & m_SessionID & " Credential: " & m_Credential.Identifier & " Is LoggingOn", TraceLevel.Verbose)

      SetState(SessionStateKind.CheckingAvailability)
      TraceToCollectedScreen("CheckingAvailability", False, True)

      TraceScreenToCollectedScreen()

      For Each IdentificationMarker As ScreenIdentificationMark In m_LogonInstructionSet.LogonInstructions.CommunicationsUnavailableIdentificationMarkers
         If IsCorrectScreen(IdentificationMarker) Then
            Dim NextTimeAvailable As Date = Date.UtcNow.AddSeconds(m_LogonInstructionSet.LogonInstructions.CommunicationsUnavailableRetryInterval)

            Dim Text As String = FormatException("Communications Unavailable Identification Marker has been found:" & IdentificationMarker.Identifier & ". Making pools unavailable untill." & NextTimeAvailable.ToLocalTime.ToString("dd/MM/yy HH:mm:ss"))
            LogToEventLog(Text, EventLogEntryType.Information)

            SessionManager.MakePoolsUnavailable(NextTimeAvailable, "CommunicationsUnavailableIdentificationMarker Found")
            Throw New SessionCommunicationUnavailableException(Text)
         End If
      Next

   End Sub

   Private Sub InitaliseEmulator()

      Trace.WriteLine("Initalising Emulator for " & ToString(), TraceLevel.Verbose)

      Try
         Monitor.Enter(m_EmulatorAccessor)
         Dim ShutDownReason As String = String.Empty
         Try

            m_Emulator = New Open3270.TNEmulator
            With m_Emulator
               If FileWriterConfig.Current.FileWriterInstanceConfigs("3270Audit").LogData Then
                  .Audit = Me
                  .Debug = True
               Else
                  .Debug = False
               End If

               With .Config
                  .AlwaysRefreshWhenWaiting = False
                  .AlwaysSkipToUnprotected = True
                  .DefaultTimeout = MQRConfig.Current.MQRServiceConfig.WinFX3270EmulatorDefaultTimeout
                  .FastScreenMode = True
                  .HostLU = m_SessionID
                  .HostName = m_LogonInstructionSet.Connection.HostAddress
                  .HostPort = m_LogonInstructionSet.Connection.HostPort
                  .IdentificationEngineOn = False
                  .IgnoreSequenceCount = True
                  .LockScreenOnWriteToUnprotected = False
                  .RefuseTN3270E = False
                  .SubmitAllKeyboardCommands = True
                  .TermType = m_LogonInstructionSet.Connection.TerminalDeviceType
                  .ThrowExceptionOnLockedScreen = True
               End With

               Trace.WriteLine("Connecting Emulator for " & ToString(), TraceLevel.Verbose)

               Dim ServerTime As Date = Now
               'round down to nearest seconds as thats all the MF can respond with
               ServerTime = New DateTime(ServerTime.Ticks - (ServerTime.Ticks Mod TimeSpan.TicksPerSecond))
               TraceToCollectedScreen("Stored Server Time ", True, True)
               .Connect()

               Trace.WriteLine("Connected Emulator for " & ToString(), TraceLevel.Verbose)
               CheckAvailabilty()

               DoLogon(ServerTime, ShutDownReason)
               If ShutDownReason = String.Empty Then
                  Trace.WriteLine("Initalised Emulator for " & ToString(), TraceLevel.Verbose)
               Else
                  ShutdownClean(True, "Logon Requested Shutdown")
               End If
            End With

            Write3270ScreenDumpToFile("Logon Success : " & (ShutDownReason = String.Empty).ToString, "Initalisation", m_SessionID)

         Finally
            Monitor.Exit(m_EmulatorAccessor)
         End Try

      Catch ex As Open3270.TNHostException When ex.Message.Contains("Unable to resolve host")
         Write3270ScreenDumpToFile("Initalisation Failure - Could not resolve host : " & ex.Message, "Initalisation", m_SessionID)
         SessionManager.MakePoolsUnavailable(Date.UtcNow.AddMinutes(5), ex.Message)

      Catch ex As ThreadAbortException
         Write3270ScreenDumpToFile("Initalisation Failure - Thread Aborted : " & ex.Message, "Initalisation", m_SessionID)
         If Not State() = SessionStateKind.ShutDownInProgress Then
            LogToEventLog("Initalisation Failure - Thread Aborted : " & ex.ToString, EventLogEntryType.Error)
         End If

      Catch ex As Open3270.TNHostException
         Write3270ScreenDumpToFile("Initalisation Failure : " & ex.ToString, "Initalisation", m_SessionID)
         If Not State() = SessionStateKind.ShutDownInProgress Then
            LogToEventLog("Initalisation Failure - Host Connectivity Issue : " & ex.ToString, EventLogEntryType.Error)
            If Not ex.Message.Contains("The TN3270 connection was lost") Then
               ShutdownClean(True, FormatException("Exception occurred while initalising Emulator!", ex))
            End If
         End If

      Catch ex As Exception
         Write3270ScreenDumpToFile("Initalisation Failure : " & ex.ToString, "Initalisation", m_SessionID)
         LogToEventLog("Initalisation Failure : " & ex.ToString, EventLogEntryType.Error)
         ShutdownClean(True, FormatException("Exception occurred while initalising Emulator!", ex))

      Finally
         m_InitalisationThread = Nothing
      End Try
   End Sub

   Private m_LastStoredScreen As String
   Private Sub TraceScreenToCollectedScreen()
      Dim NewScreen As String = VisibleScreen()
      If Not m_LastStoredScreen = NewScreen Then
         TraceToCollectedScreen("", False, True)
         TraceToCollectedScreen(NewScreen, False, False)
         TraceToCollectedScreen("", False, True)
         m_LastStoredScreen = NewScreen
      End If
   End Sub

   Private Sub Write3270ScreenDumpToFile(ByVal AdditionalData As String, ByVal Query As String, ByVal Parameters As String)

      m_LastScreenSet = ControlChars.NewLine & Now().ToString("dd/MM/yy HH:mm:ss")
      m_LastScreenSet += ControlChars.NewLine & ToString()
      m_LastScreenSet += ControlChars.NewLine & m_LogonInstructionSet.Identifier
      m_LastScreenSet += ControlChars.NewLine & Query & " : " & Parameters
      m_LastScreenSet += ControlChars.NewLine + AdditionalData
      m_LastScreenSet += ControlChars.NewLine
      TraceToCollectedScreen("", False, True)
      TraceToCollectedScreen("END", False, True)
      TraceToCollectedScreen("", False, True)
      m_LastScreenSet += GetScreenTrace(False)
      m_LastScreenSet += ControlChars.NewLine

      FileWriter.Write("3270ScreenDump", m_LastScreenSet, Query, CreateSafeFilePrefix(Parameters) & "_")

      m_CollectedScreenTrace = New StringBuilder
   End Sub

   Private Function IsCorrectScreen(ByVal IdentificationMarkers As ScreenIdentificationMarks, ByRef FailureMark As String, ByVal StoreFailure As Boolean) As Boolean
      Dim Result As Boolean = False

      For Each IdentificationMarker As ScreenIdentificationMark In IdentificationMarkers
         Result = IsCorrectScreen(IdentificationMarker)

         If Not Result And StoreFailure Then
            With IdentificationMarker
               FailureMark = .Identifier

               TraceToCollectedScreen("", False, False)
               TraceToCollectedScreen("", False, True)
               TraceToCollectedScreen("SCREEN IDENTIFACTION FAILURE", False, True)
               TraceToCollectedScreen("INCORRECT SCREEN : " & .Identifier, False, True)
               TraceToCollectedScreen("INCORRECT SCREEN REGEX : " & .RegexPattern.Value, False, True)
               TraceToCollectedScreen("INCORRECT SCREEN AREA : " & m_Emulator.CurrentScreenXML.GetText(.IdentificationArea.StartRow, .IdentificationArea.EndRow, .IdentificationArea.StartCol, .IdentificationArea.EndCol), False, True)
               TraceToCollectedScreen("", False, True)
               TraceToCollectedScreen("", False, False)

            End With
            Exit For
         End If

      Next

      Return Result
   End Function

   Private Sub CheckErrorMarkers(ByVal IdentificationMarkers As ErrorIdentificationMarks)

      For Each IdentificationMarker As ErrorIdentificationMark In IdentificationMarkers
         If IdentificationMarker.IsMatch(m_Emulator.CurrentScreenXML.GetText) Then
            SessionManager.MakePoolsUnavailable(Date.UtcNow.AddSeconds(IdentificationMarker.PoolUnavailablePeriod), "Error Condition:" & IdentificationMarker.Identifier & " was found!")
            Throw New ErrorIdentifactionMarkerFoundException("Found a Error Identification Marker " & IdentificationMarker.Identifier)
         End If
      Next

   End Sub

   Private Function IsCorrectScreen(ByVal IdentificationMarker As ScreenIdentificationMark) As Boolean
      Dim Result As Boolean = True

      With IdentificationMarker
         If .WaitPeriod = -1 Then
            Result = m_Emulator.WaitForText(.IdentificationArea.StartRow, .IdentificationArea.EndRow, .IdentificationArea.StartCol, .IdentificationArea.EndCol, .RegexPattern.Value, .RegexPattern.Options, 0)
         Else
            Result = m_Emulator.WaitForText(.IdentificationArea.StartRow, .IdentificationArea.EndRow, .IdentificationArea.StartCol, .IdentificationArea.EndCol, .RegexPattern.Value, .RegexPattern.Options, .WaitPeriod)
         End If
      End With

      Return Result
   End Function

   Private Sub WriteInput(ByVal Input As ScreenInput, Optional ByVal GetValueMethod As GetVariableValueDelegate = Nothing)
      With Input
         Dim ValueToWrite As String = .GetInputValue(GetValueMethod)
         TraceToCollectedScreen("Writing " & Input.Identifier & " to screen:" & ValueToWrite, True, True)
         If .FieldNumber = -1 Then
            m_Emulator.SetCursor(.XPos, .YPos)
            m_Emulator.SendText(ValueToWrite)
         Else
            m_Emulator.SetField(.FieldNumber, ValueToWrite)
         End If
      End With
   End Sub

   Private Sub WriteInput(ByVal Inputs As ScreenInputs, Optional ByVal GetValueMethod As GetVariableValueDelegate = Nothing)

      Dim HaveWitten As Boolean

      If Inputs.Count > 0 Then

         'TraceToCollectedScreen("Writing " & Inputs.Count & " Fields to screen.", True, True)

         For Each Input As ScreenInput In Inputs
            WriteInput(Input, GetValueMethod)
            HaveWitten = True
         Next

         If HaveWitten Then
            TraceScreenToCollectedScreen()
         End If
      End If

   End Sub

   Private Sub ExtractCapturePoints(ByVal Points As ScreenCaptureDataPoints)

      TraceToCollectedScreen("", False, True)
      For Each Point As ScreenCaptureDataPoint In Points
         ExtractCapturePoint(Point)
      Next
      TraceToCollectedScreen("", False, True)

   End Sub

   Private Sub ExtractCapturePoint(ByVal Point As ScreenCaptureDataPoint)

      TraceScreenToCollectedScreen()

      Dim Target As Dictionary(Of String, String)
      Dim DataType As String

      Select Case m_State
         Case SessionStateKind.CheckingAvailability, SessionStateKind.Inital, SessionStateKind.LoggingOn
            Target = m_LogonCapturedData
            DataType = "Logon"
         Case Else
            Target = m_QueryCapturedData
            DataType = "Query"
      End Select

      With Point
         Dim CapturedValue As String = ""
         CapturedValue = m_Emulator.CurrentScreenXML.GetText(.IdentificationArea.StartRow, .IdentificationArea.EndRow, .IdentificationArea.StartCol, .IdentificationArea.EndCol, .RegexPattern.Value, .RegexPattern.Options)
         If Target.ContainsKey(.Identifier) Then
            Target(.Identifier) = CapturedValue
         Else
            Target.Add(.Identifier, CapturedValue)
         End If

         If Point.ExceptIfNotFound And CapturedValue.Trim = String.Empty Then
            Throw New InvalidExpressionException("Could not find any data for " & Point.Identifier & " Text Searched was: " & m_Emulator.CurrentScreenXML.GetText(.IdentificationArea.StartRow, .IdentificationArea.EndRow, .IdentificationArea.StartCol, .IdentificationArea.EndCol))
         End If

         TraceToCollectedScreen("CAPTURED " & DataType & " DATA:" & (.Identifier & " = ").PadRight(15, " "c) & CapturedValue, False, True)

      End With
   End Sub

   Private Function WaitForScreens(ByVal IdentificationMarkers As ScreenIdentificationMarks, ByVal Timeout As Integer) As Boolean

      For Each Marker As ScreenIdentificationMark In IdentificationMarkers
         If Not WaitForScreen(Marker, Timeout) Then
            Return False
         End If
      Next

      Return True

   End Function

   Private Function WaitForScreen(ByVal IdentificationMarker As ScreenIdentificationMark, ByVal Timeout As Integer) As Boolean
      With IdentificationMarker
         Return m_Emulator.WaitForText(.IdentificationArea.StartRow, .IdentificationArea.EndRow, .IdentificationArea.StartCol, .IdentificationArea.EndCol, .RegexPattern.Value, .RegexPattern.Options, Timeout)
      End With
   End Function

   Private Function GetUserNamePasswordFromCredentials(ByVal VariableName As String) As String
      Dim Result As String = ""

      Select Case VariableName.ToUpper

         Case "UserName".ToUpper
            Return m_Credential.UserName

         Case "Password".ToUpper
            Return m_Credential.Password

         Case "NewPassword".ToUpper
            Return m_Credential.NewPassword

      End Select

      Return Result
   End Function

   Private Function DoPasswordChange(ByVal ChangeInstructions As LogonPasswordChange, ByVal Navigationaction As NavigationAction, ByRef ShutDownReason As String, MFTransData As MFTransaction.TransactionData) As Boolean
      Dim Result As Boolean


      If DateDiff(DateInterval.Day, m_Credential.PasswordChangedDate, Now) > LogonCredentialManager.PasswordChangeIntervalDays Then
         TraceToCollectedScreen("CHANGING PASSWORD", False, True)

         Dim GetValueMethod As New GetVariableValueDelegate(AddressOf GetUserNamePasswordFromCredentials)

         With ChangeInstructions
            WriteInput(.Inputs, GetValueMethod)

            DoNavigation(MFTransData, Navigationaction, "PasswordChange")
            DoProcessActions(MFTransData, .Actions, "PasswordChange", False, ShutDownReason, GetValueMethod)
         End With

         If Not ShutDownReason = String.Empty Then
            Result = False
            LogToEventLog("PasswordChange Failed! " & ShutDownReason, EventLogEntryType.Warning)
         Else
            m_Credential.SaveNewPassword()
            Dim Msg As String = "Credential: " & m_Credential.Identifier & " Changed Password"
            Trace.WriteLine(Msg, TraceLevel.Verbose)
            LogToEventLog(Msg, EventLogEntryType.Information)
            Result = True
         End If
      Else
         DoNavigation(MFTransData, Navigationaction, "Logon")
         Result = True
      End If

      Return Result

   End Function

   Private Sub DoProcessAction(ByRef Transaction As MFTransaction.TransactionData, ByVal Action As ProcessAction, Task As String, ByRef WaitForResponse As Boolean, ByRef ShutDownReason As String, Optional ByVal GetValueMethod As GetVariableValueDelegate = Nothing)

      With Action

         CheckErrorMarkers(.ErrorScreenIdentificationMarkers)

         Dim FailureMark As String = String.Empty
         If IsCorrectScreen(.ScreenIdentificationMarkers, FailureMark, .IsCoreAction) Then
            ExtractCapturePoints(.CapturePoints)

            If .ParseScreen Then
               If m_QueryCapturedData.ContainsKey("QueryScreen") Then
                  m_QueryCapturedData("QueryScreen") += vbCrLf & VisibleScreen()
               Else
                  m_QueryCapturedData.Add("QueryScreen", VisibleScreen)
               End If
            End If

            WriteInput(.Inputs, GetValueMethod)
            DoNavigation(Transaction, .NavigationAction, .Identifier)
            WaitForResponse = Not .NoData

            DoProcessActions(Transaction, .Actions, Task, WaitForResponse, ShutDownReason, GetValueMethod)
            If .LockCredential Then
               m_Credential.LockOut()
               Dim Message As String = "Locked Out credential " & m_Credential.Identifier & " by action " & .Identifier & ControlChars.NewLine & Me.GetScreenTrace(True)
               LogToEventLog(Message, EventLogEntryType.Warning)
            End If
         Else
            If .IsCoreAction Then
               ShutDownReason = "Failed Core Action for " & Action.Identifier & " due to Incorrect screen: " & FailureMark & " "
               TraceToCollectedScreen("SHUTDOWN", False, True)
               TraceToCollectedScreen(ShutDownReason, False, False)
            End If
         End If

      End With
   End Sub

   Private Function CheckSuccessConditions(ByVal Conditions As SuccessConditions, ByVal LogFailure As Boolean) As Boolean

      Dim Result As Boolean = True

      For Each Condition As SuccessCondition In Conditions
         If Not CheckSuccessCondition(Condition, LogFailure) Then
            Result = False
            Exit For
         End If
      Next

      Return Result
   End Function

   Private Function CheckSuccessCondition(ByVal Condition As SuccessCondition, ByVal LogFailure As Boolean) As Boolean

      Dim FailureMark As String = String.Empty
      If IsCorrectScreen(Condition.IdentifactionMarks, FailureMark, LogFailure) Then
         Return True
      Else
         If LogFailure Then
            Dim Message As String = "Processing failed due to failure of success condition:" & FailureMark & ControlChars.NewLine & VisibleScreen()
            Trace.WriteLine(Message, TraceLevel.Verbose)
            LogToEventLog(Message, EventLogEntryType.Warning)
         End If
         Return False
      End If
   End Function

   Private Sub DoProcessActions(ByRef Transaction As MFTransaction.TransactionData, ByVal Actions As ProcessActions, Task As String, ByRef WaitForResponse As Boolean, ByRef ShutDownReason As String, Optional ByVal GetValueMethod As GetVariableValueDelegate = Nothing)

      For Each Item As ProcessAction In Actions
         DoProcessAction(Transaction, Item, Task & "-" & Item.Identifier, WaitForResponse, ShutDownReason, GetValueMethod)
         If Not ShutDownReason = String.Empty Then
            Exit For
         End If
      Next

   End Sub

   Private Sub SendKeyCommand(ByRef Data As MFTransaction.TransactionData, ByVal Command As KeyCommand, ByVal TimeOutMilliseconds As Integer, ByVal Task As String, ByVal ScreenRefreshes As Integer)

      Dim SubmitTime As DateTime = Date.UtcNow

      TraceToCollectedScreen("", False, True)
      TraceToCollectedScreen("NAVIGATION KEY = " & Command.ToString, True, True)
      TraceToCollectedScreen("", False, True)

      If ScreenRefreshes > 0 Then
         TraceToCollectedScreen("", False, True)
         TraceToCollectedScreen("WAIT FOR " & ScreenRefreshes.ToString & " Screeen Refreshes", False, True)
         TraceToCollectedScreen("", False, True)

         Dim BeforeScreen As String = VisibleScreen()
         If m_Emulator.SendKey(True, ConvertCommand(Command), TimeOutMilliseconds) Then
            MFTransaction.WriteTransactionDetails(Data, Task, Command)
            Dim TimeoutTime As Date = Date.UtcNow.AddMilliseconds(TimeOutMilliseconds)
            Dim TimedOut As Boolean = True
            Dim AfterScreen As String = VisibleScreen()
            Dim TimesScreenAltered As Integer = 0

            Dim TimesWaited As Integer = 0

            Do
               If Not BeforeScreen = AfterScreen Then
                  TraceToCollectedScreen("SCREEN ALTERED", True, True)
                  TraceToCollectedScreen("", False, True)
                  TraceScreenToCollectedScreen()
                  TimesScreenAltered += 1
                  BeforeScreen = AfterScreen
                  If TimesScreenAltered = ScreenRefreshes Then
                     TimedOut = False
                     Exit Do
                  End If
               End If

               Dim WaitMultiplier As Integer = (TimesWaited Mod 5) + 1
               Dim WaitPeriod As Integer = WaitMultiplier * 100

               TraceToCollectedScreen("WAITING " & (WaitPeriod / 1000) & " Sec(s) before refresh", True, True)
               System.Threading.Thread.Sleep(WaitPeriod)
               RefreshEmulatorScreen()
               AfterScreen = VisibleScreen()
               TimesWaited += 1

            Loop While (Date.UtcNow < TimeoutTime)

            If TimedOut Then
               Dim ErrorText As String = "SCREEN DID NOT REFRESH. TASK : " & Task & " AFTER: " & (TimeOutMilliseconds / 1000) & " seconds."
               TraceToCollectedScreen("", False, False)
               TraceToCollectedScreen("", False, True)
               TraceToCollectedScreen(ErrorText, False, True)
               TraceToCollectedScreen("", False, True)
               TraceToCollectedScreen("", False, False)
               Throw New ScreenNavigationException(ErrorText)
            Else
               TraceScreenToCollectedScreen()
            End If
         Else
            Throw New ScreenNavigationException("Key Command Failed:" & Command.ToString & " in " & Task)
         End If
      Else
         m_Emulator.SendKey(False, ConvertCommand(Command))
      End If

   End Sub

   Private Sub DoNavigation(ByRef Data As MFTransaction.TransactionData, ByRef NavigationAction As NavigationAction, ByVal Task As String)

      If NavigationAction.NavigationWaitMilliseconds > 0 Then
         SendKeyCommand(Data, NavigationAction.NavigationKey, NavigationAction.NavigationTimeoutMilliseconds, Task, 0)
         TraceToCollectedScreen("", False, True)
         TraceToCollectedScreen("WAIT MILLISECONDS = " & NavigationAction.NavigationWaitMilliseconds.ToString, True, True)
         TraceToCollectedScreen("", False, True)
         Thread.Sleep(NavigationAction.NavigationWaitMilliseconds)
         RefreshEmulatorScreen()
      Else
         SendKeyCommand(Data, NavigationAction.NavigationKey, NavigationAction.NavigationTimeoutMilliseconds, Task, NavigationAction.ScreenRefreshes)
      End If

      TraceScreenToCollectedScreen()

   End Sub

   Private Sub RefreshEmulatorScreen(Optional ByVal TimeoutMS As Integer = -1)

      TraceToCollectedScreen("SCREEN REFRESH", True, True)
      If TimeoutMS = -1 Then
         m_Emulator.Refresh()
      Else
         m_Emulator.Refresh(True, TimeoutMS)
      End If

   End Sub

   Private Function ConvertCommand(ByVal Command As KeyCommand) As Open3270.KeyCommand
      Dim Result As Open3270.KeyCommand
      Result = DirectCast([Enum].Parse(GetType(Open3270.KeyCommand), Command.ToString), Open3270.KeyCommand)
      Return Result
   End Function

   Private Sub SetState(ByVal value As SessionStateKind)

      Select Case m_State
         Case value
            'do nothing tryign to set the same state
         Case SessionStateKind.ShutDownInProgress
            'once we are shutting down thats it 
            'do nothing
         Case Else
            m_LastStateChangeUTC = Date.UtcNow
            m_State = value
            Trace.WriteLine(value.ToString & " State Set on: " & ToString(), TraceLevel.Verbose)

      End Select

   End Sub

   Private ReadOnly Property NotificationData() As NotifyData
      Get
         Return EWS.Diagnostics.NotificationManager.Instance(NOTIFY_CATEGORY, m_LogonInstructionSet.Identifier)
      End Get
   End Property

   Private Function Status() As String
      If m_State = SessionStateKind.Processing Then
         Return m_State.ToString & " " & m_ProcessingQuery & " - " & Elapsed()
      Else
         Return m_State.ToString & " - " & Elapsed()
      End If

   End Function

   Private Sub LogToEventLog(ByVal Message As String, ByVal Level As EventLogEntryType)
      EventLogging.Log(ToString() & vbCrLf & vbCrLf & Message, Me.GetType.Name, Level)
   End Sub

   Private Sub TraceToCollectedScreen(ByVal Text As String, ByVal IncludeDate As Boolean, ByVal PaddOut As Boolean)

      Static LastText As String

      If Not Text = LastText Then

         LastText = Text

         If IncludeDate Then
            Text = Now.ToString("yyyy-MM-dd HH:mm:ss.fff") & " : " & Text
         End If

         If PaddOut Then

            If Not IncludeDate And Not Text = String.Empty Then
               Text = "".PadLeft(26, " "c) & Text
            End If

            If Text = "" Then
               Text = Text.PadRight(83, "-"c)
            Else
               Text = ("----- " & Text.PadRight(63, " "c) & " ").PadRight(83, "-"c)
            End If
         End If

         m_CollectedScreenTrace.AppendLine(Text)
         Debug.WriteLine(Text)
      End If
   End Sub

#Region " Audit "

   Public Sub Write(ByVal text As String) Implements Open3270.IAudit.Write
      FileWriter.Write("WinFX3270Audit", text)
   End Sub

   Public Sub WriteLine(ByVal text As String) Implements Open3270.IAudit.WriteLine
      FileWriter.Write("WinFX3270Audit", text)
   End Sub

#End Region

#End Region

#Region " Overrides "

   Public Overrides Function ToString() As String
      Return String.Format("ID:{0} TOPSName:{1} TOPSAltName:{2} Credential:{3} State:{4} LockedTimeout:{5}", m_SessionID, TOPSName, TOPSAltName, CredentialName, State.ToString, LockedTimeout)
   End Function

#End Region

End Class


