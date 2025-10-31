Imports EWS.Diagnostics
Imports System.Threading
Imports EWS.MQR.XML
Imports EWS.MQR.XML.InstructionSetManager

Public Class SessionManager

   Private Shared m_RetriveSessionLock As New Object
   Private Shared m_SessionCheckerAborted As Boolean = False

   Private Shared m_ShutDownQueue As New Specialized.StringDictionary()

   Private Shared m_RequestToSessionIndexer As New SafeDictionary(Of Guid, String)
   Private Shared m_SessionPoolIndexer As New Dictionary(Of String, String)
   Private Shared m_SessionPools As New Dictionary(Of String, SessionPool)


   Public Shared Function GetSessionStates() As String

      Try
         Dim State As New SessionManagerStateData(InstructionSetManager.Requires3270Sessions)

         If InstructionSetManager.Requires3270Sessions Then
            For Each InstructionSetName As String In InstructionSetManager.LogonInstructionSetKeys
               If Not InstructionSetManager.LogonInstructionSetsToUseMFFor.Contains(InstructionSetName.ToUpper) Then
                  Dim SessionPoolState As New SessionPoolStateDataItem()
                  With SessionPoolState
                     .Identifier = InstructionSetName
                     .CacheOnly = True
                     .Available = True
                     .UnavailableReason = ""
                     .UnavailableUntill = Date.MinValue
                  End With

                  State.SessionPoolSates.Add(InstructionSetName, SessionPoolState)
               Else
                  Dim Pool As SessionPool = m_SessionPools(InstructionSetName)
                  State.SessionPoolSates.Add(Pool.PoolID, Pool.GetStateData)
               End If
            Next

            State.SessionPoolSates.Add("UnavailableSessions", InstructionSetManager.GetUnavailableSessions)
            State.SessionStarterThreadState = SessionStarterThreadState()

         End If

         Return State.ToXML(False)
      Catch e As Exception
         EventLogging.Log(e.ToString, "SessionManger", EventLogEntryType.Warning)
         Return String.Empty
      End Try

   End Function

   Public Shared Function SessionStarterThreadState() As String
      Return EWS.Diagnostics.NotificationManager.Instance(SessionInstance.NOTIFY_CATEGORY, "SessionChecker").State
   End Function

   Public Shared Sub RequestComplete(requestId As Guid)
      m_RequestToSessionIndexer.Remove(requestId)
   End Sub

   Public Shared Function GetSessionData(ByVal SessionID As String) As String

      Dim Session As SessionInstance = GetSessionBySessionID(SessionID)
      Return Session.GetSessionData.ToXML(False)

   End Function

   Public Shared Function GetCurrentScreenTraceBySessionID(ByVal SessionID As String) As String
      Return GetSessionBySessionID(SessionID).GetScreenTrace(False)
   End Function

   Public Shared Function GetCurrentScreenTraceByTOPSName(ByVal TOPSName As String) As String
      Return GetSessionByTOPSName(TOPSName).GetScreenTrace(False)
   End Function

   Public Shared Function GetCurrentScreenBySessionID(ByVal SessionID As String) As String
      Return GetSessionBySessionID(SessionID).VisibleScreen
   End Function

   Public Shared Function GetCurrentScreenByTOPSName(ByVal TOPSName As String) As String
      Return GetSessionByTOPSName(TOPSName).VisibleScreen
   End Function

   Public Shared Function GetLastScreenSetBySessionID(ByVal SessionID As String) As String
      Return GetSessionBySessionID(SessionID).LastScreenSet
   End Function

   Public Shared Function GetLastScreenSetByTOPSName(ByVal TOPSName As String) As String
      Return GetSessionByTOPSName(TOPSName).LastScreenSet
   End Function

   Public Shared Sub MakePoolUnavailable(ByVal PoolName As String, ByVal AvailableAfterUtc As Date, ByVal Reason As String)
      m_SessionPools(PoolName.ToUpper).MakeUnavailable(AvailableAfterUtc, Reason)
   End Sub

   Public Shared Sub MakePoolAvailable(ByVal PoolName As String)
      m_SessionPools(PoolName.ToUpper).MakeAvailable()
   End Sub

   Public Shared Sub MakePoolsUnavailable(ByVal AvailableAfterUtc As Date, ByVal Reason As String)
      For Each Pool As SessionPool In m_SessionPools.Values
         Pool.MakeUnavailable(AvailableAfterUtc, Reason)
      Next
   End Sub

   Public Shared Sub MakePoolsAvailable()

      For Each Pool As SessionPool In m_SessionPools.Values
         Pool.MakeAvailable()
      Next

   End Sub

   Public Shared Sub CloseAllSessions(ByVal Reason As String)

      For Each Pool As SessionPool In m_SessionPools.Values
         Pool.CloseAllSessions(Reason)
      Next

   End Sub

   Public Shared Sub QueueSessionShutdown(RequestID As Guid, Reason As String)
      If m_RequestToSessionIndexer.ContainsKey(RequestID) Then
         QueueSessionShutdown(m_RequestToSessionIndexer(RequestID), Reason)
         m_RequestToSessionIndexer.Remove(RequestID)
      End If

   End Sub

   Public Shared Sub QueueSessionShutdown(ByVal SessionID As String, ByVal Reason As String)
      Monitor.Enter(m_ShutDownQueue)
      Try
         If Not m_ShutDownQueue.ContainsKey(SessionID) Then
            Trace.WriteLine("3270Session:" & SessionID & " queued closed down because: " & Reason, TraceLevel.Verbose)
            m_ShutDownQueue.Add(SessionID, Reason)
         End If
      Finally
         Monitor.Exit(m_ShutDownQueue)
      End Try

      Try
         GetSessionBySessionID(SessionID).QueuedShutdown(Reason)
      Catch ex As Exception
         'it does not matter its queued for shutdown anyhow
      End Try

      QueueManager.EmptyQueuesForSession(SessionID, Reason)

   End Sub

   Private Shared Sub ShutdownSessionsQueuedForShutdown()

      Monitor.Enter(m_ShutDownQueue)
      Try
         For Each SessionID As String In m_ShutDownQueue.Keys
            Try
               Dim Session As SessionInstance = Nothing
               Try
                  'this could except if ic ant find it
                  Session = GetSessionBySessionID(SessionID)
               Catch e As Exception
                  'ignore it we just cant shutdown
               End Try
               If Not Session Is Nothing Then
                  ShutdownSession(Session, m_ShutDownQueue(SessionID))
               End If
            Catch ex As Exception
               EventLogging.Log("Exception shutting down Session " & SessionID & " - " & ex.ToString, "SessionManager", EventLogEntryType.Error)
            End Try
         Next
      Catch ex As Exception
         EventLogging.Log("Exception Shutting Down Sessions " & ex.ToString, "SessionManager", EventLogEntryType.Error)
      Finally
         m_ShutDownQueue.Clear()
         Monitor.Exit(m_ShutDownQueue)
      End Try


   End Sub

   Private Shared Sub ShutdownSession(ByVal SessionItem As SessionInstance, ByVal Reason As String)

      Dim SessionID As String = SessionItem.SessionID
      Dim PoolName As String = SessionItem.PoolName.ToUpper
      Dim TOPSAltName As String = SessionItem.TOPSAltName

      'Remove it from the Pools
      m_SessionPools(PoolName).Remove(SessionID)
      m_SessionPoolIndexer.Remove(SessionID)

      'CloseDown The Session
      SessionItem.TerminateSession(Reason)

      'Remove any item that relates to this session from the awaiting output queue
      QueueManager.AwaitingOutputQueueInstance.Dequeue(TOPSAltName)

      SessionItem.ReleaseSeeionUseFromLogonInstructionSet()

      SessionItem = Nothing
      GC.Collect()

   End Sub

   Public Shared Sub ResetSessionCountersBySessionID(ByVal SessionID As String)
      Dim Session As SessionInstance = GetSessionBySessionID(SessionID)
      Session.ResetCounters()
   End Sub

   Public Shared Sub ResetSessionCountersByTOPSName(ByVal TOPSName As String)
      Dim Session As SessionInstance = GetSessionByTOPSName(TOPSName)
      Session.ResetCounters()
   End Sub

   Public Shared Sub ResetAllSessionCounters()
      For Each Pool As SessionPool In m_SessionPools.Values
         For Each SessionItem As SessionInstance In Pool.Values
            SessionItem.ResetCounters()
         Next
      Next

   End Sub

   Public Shared Function ReceivedSessionResponse(ByVal TOPSAltName As String, ByVal Data As String) As Dictionary(Of String, String)
      Return GetSessionByTOPSName(TOPSAltName).ReceivedResponse(Data)
   End Function

   Public Shared Sub InitaliseSessions()

      If InstructionSetManager.Requires3270Sessions Then
         ShutDown()
         For Each InstructionSetName As String In InstructionSetManager.LogonInstructionSetKeys
            If InstructionSetManager.LogonInstructionSetsToUseMFFor.Contains(InstructionSetName.ToUpper) Then
               Dim InstructionSet As LogonInstructionSet = InstructionSetManager.GetLogonInstructionset(InstructionSetName)
               Dim NewPool As New SessionPool(InstructionSet.Identifier)
               m_SessionPools.Add(InstructionSet.Identifier.ToUpper, NewPool)
               NotificationManager.Instance(SessionInstance.NOTIFY_CATEGORY, InstructionSet.Identifier).SetStateDelegate(New GetNotifyDataValue(AddressOf NewPool.ItemCount))
            End If
         Next
         StartSessionChecker()
      End If

   End Sub

   Public Shared Sub ShutDown()

      If InstructionSetManager.Requires3270Sessions Then
         StopSessionChecker()

         ShutdownSessionsQueuedForShutdown()

         For Each Pool As SessionPool In m_SessionPools.Values
            Dim Items As New Collection
            For Each Session As SessionInstance In Pool.Values
               Items.Add(Session)
            Next

            For Each Session As SessionInstance In Items
               Try
                  ShutdownSession(Session, "Service Shutdown")
               Catch ex As Exception
                  EventLogging.Log("Exception during shutdown:" & ex.ToString, "SessionManager", EventLogEntryType.Warning)
               End Try
            Next
            Pool.Clear()
            EWS.Diagnostics.NotificationManager.RemoveProcess(SessionInstance.NOTIFY_CATEGORY, Pool.PoolID)
         Next

         m_SessionPools.Clear()
         m_SessionPoolIndexer.Clear()
         m_ShutDownQueue.Clear()

         NotificationManager.RemoveProcess(SessionInstance.NOTIFY_CATEGORY, "SessionChecker")

      End If

   End Sub

   Public Shared Function GetFreeSessionForQuery(ByVal Request As RequestItem) As SessionInstance

      Dim InstructionSet As LogonInstructionSet = InstructionSetManager.GetLogonInstructionset(Request.LogonIdentifier)
      Dim Result As SessionInstance = Nothing

      If m_SessionPools(InstructionSet.Identifier.ToUpper).Available Then
         Do While Result Is Nothing
            For count As Integer = 1 To 10
               Monitor.Enter(m_RetriveSessionLock)
               Try
                  For Each Item As SessionInstance In m_SessionPools(InstructionSet.Identifier.ToUpper).Values
                     If (Not Item.IsLocked) AndAlso (Item.State = SessionStateKind.ReadyForCommand) Then
                        If IsSessionHealthlyResetIfNot(Item) Then
                           If Result Is Nothing Then
                              Result = Item
                           Else
                              Select Case True
                                 Case Item.Priority < Result.Priority
                                    Result = Item

                                 Case Item.Priority = Result.Priority
                                    If Item.LastUsedUTC < Result.LastUsedUTC Then
                                       Result = Item
                                    End If

                              End Select
                           End If
                        End If
                     End If
                  Next

                  If Not Result Is Nothing Then
                     Result.LockSession("Retrieve Session For Query")
                     Trace.WriteLine("Retrieved Session " & Result.ToString, TraceLevel.Verbose)
                  End If

               Finally
                  Monitor.Exit(m_RetriveSessionLock)
               End Try

               If Result Is Nothing Then
                  If count = 10 Then
                     Throw New NoFreeSessionsException("Could not obtain an session for:" & InstructionSet.Identifier)
                  Else
                     Thread.Sleep(CInt(MQRConfig.Current.MQRServiceConfig.SessionWaitTimeOut / 10))
                  End If
               Else
                  Exit For
               End If
            Next
         Loop
      Else
         With m_SessionPools(InstructionSet.Identifier.ToUpper)
            Throw New NoFreeSessionsException("Could not obtain an session for: " & InstructionSet.Identifier & " because it is unavailable due to " & .UnavailablityReason & " will not be Available again until: " & .AvailableAfterLocal.ToString("dd/MM/yy HH:mm:ss"))
         End With
      End If

      If Not Result Is Nothing Then
         m_RequestToSessionIndexer.Add(Request.Request.ID, Result.SessionID)
      End If

      Return Result
   End Function

   Private Shared Function GetSessionBySessionID(ByVal SessionID As String) As SessionInstance
      SessionID = SessionID.ToUpper

      If m_SessionPoolIndexer.ContainsKey(SessionID) Then
         Dim PoolName As String = m_SessionPoolIndexer(SessionID).ToUpper
         If m_SessionPools.ContainsKey(PoolName) Then
            If m_SessionPools(PoolName.ToUpper).ContainsKey(SessionID) Then
               Return m_SessionPools(PoolName)(SessionID)
            Else
               Throw New CouldNotFindSessionException("Could Not Find Session " & SessionID)
            End If
         Else
            Throw New CouldNotFindSessionException("Could Not Find Session " & SessionID)
         End If
      Else
         Throw New CouldNotFindSessionException("Could Not Find Session " & SessionID)
      End If
   End Function

   Public Shared Function GetSessionByTOPSName(ByVal TOPSName As String) As SessionInstance

      TOPSName = TOPSName.ToUpper

      Dim Result As SessionInstance = Nothing

      For Each Pool As SessionPool In m_SessionPools.Values
         For Each SessionItem As SessionInstance In Pool.Values
            If SessionItem.IsTOPSName(TOPSName) Then
               Result = SessionItem
               Exit For
            End If
         Next
         If Not Result Is Nothing Then
            Exit For
         End If
      Next

      If Result Is Nothing Then
         Throw New CouldNotFindSessionException("Could Not Find Session for TOPSName " & TOPSName)
      Else
         Return Result
      End If

   End Function

   Private Shared Sub StartSessionCheckerThread()

      m_SessionCheckerAborted = False
      SessionCheckerNotificationData.UpdateState(ProcessState.Running)
      SessionCheckerNotificationData.SetDelegate("CreatedSessions", New GetNotifyDataValue(AddressOf CreatedSessionsCount))
      SessionCheckerNotificationData.SetDelegate("UnhealthySessions", New GetNotifyDataValue(AddressOf UnhealthySessionsCount))
      SessionCheckerNotificationData.SetDelegate("ShutDownQueueLenght", New GetNotifyDataValue(AddressOf ShutDownQueueLenght))

      ThreadManager.Thread(SessionCheckerThreadName, New ThreadManager.DoWork(AddressOf SessionChecker), ThreadPriority.AboveNormal, 5000, 5000, True, True)

   End Sub

   Private Shared ReadOnly Property SessionCheckerThreadName() As String
      Get
         Return "SessionChecker"
      End Get
   End Property

   Private Shared m_CreatedSessions As Long
   Private Shared Function CreatedSessionsCount() As String
      Return m_CreatedSessions.ToString
   End Function

   Private Shared m_UnhealthySessions As Long
   Private Shared Function UnhealthySessionsCount() As String
      Return m_UnhealthySessions.ToString
   End Function

   Private Shared Function ShutDownQueueLenght() As String
      Return m_ShutDownQueue.Count.ToString
   End Function

   Private Shared Sub CreateSessionsInPoolsWithoutEnoughAllocated()

      For Each InstructionSetName As String In InstructionSetManager.LogonInstructionSetKeys
         If m_SessionCheckerAborted Then Exit For

         If Not m_LastTooFewSessionAvailableMailSentUTC.ContainsKey(InstructionSetName.ToUpper) Then
            m_LastTooFewSessionAvailableMailSentUTC.Add(InstructionSetName.ToUpper, Date.MinValue)
         End If

         If m_SessionPools.ContainsKey(InstructionSetName.ToUpper) AndAlso m_SessionPools(InstructionSetName.ToUpper).Available Then
            Dim InstructionSet As LogonInstructionSet = InstructionSetManager.GetLogonInstructionset(InstructionSetName)

            With InstructionSet.StaticConfiguration
               If m_SessionPools(InstructionSetName.ToUpper).Count < .ConnectionCount Then
                  For count As Integer = 1 To .ConnectionCount - m_SessionPools(InstructionSet.Identifier.ToUpper).Count
                     If m_SessionCheckerAborted Then
                        Exit For
                     End If

                     If InstructionSet.HasAvailableSession Then
                        If CreateSession(InstructionSet) Then
                           m_CreatedSessions += 1
                        End If
                     Else
                        'send the email telling people
                        SendTooFewSessionAvailableAltertEmail(m_SessionPools(InstructionSet.Identifier.ToUpper).Count, .ConnectionCount, InstructionSet.Identifier)
                        Exit For
                     End If
                  Next
               Else
                  'all is ok so make sure we alter if things go wrong again.
                  m_LastTooFewSessionAvailableMailSentUTC(InstructionSetName.ToUpper) = Date.MinValue
               End If
            End With
         End If
      Next

   End Sub

   Private Shared Sub ReleaseInstructionSetSessionsInUseWhereSessionNotInUse()

      For Each InstructionSetName As String In InstructionSetManager.GetAllLogonInstructionsetNames
         Dim InstructionSet As LogonInstructionSet = InstructionSetManager.GetLogonInstructionset(InstructionSetName)

         If m_SessionPools.ContainsKey(InstructionSetName.ToUpper) Then
            Dim Pool As SessionPool = m_SessionPools(InstructionSetName.ToUpper)
            For Each Sess As LogonSession In InstructionSet.Sessions.Values
               If Not Pool.ContainsKey(Sess.SessionID) Then
                  If Sess.InUse Then
                     If Sess.LastUsedUTC.AddSeconds(30) < Date.UtcNow Then
                        Sess.ReleaseUse()
                        Dim Message As String = "Released session " & Sess.SessionID & " that was in used but could not have been as it was not in a pool!"
                        EventLogging.Log(Message, "SessionManager", EventLogEntryType.Warning)
                        EWS.Diagnostics.Trace.WriteLine(Message, TraceLevel.Info)
                     End If
                  End If
               End If
            Next
         Else
            'release them all....
            For Each Sess As LogonSession In InstructionSet.Sessions.Values
               If Sess.InUse Then
                  'nothing should be inuse over 15 mins
                  Sess.ReleaseUse()
                  Dim Message As String = "Released session " & Sess.SessionID & " that was in used but we have no session pool for " & InstructionSetName
                  EventLogging.Log(Message, "SessionManager", EventLogEntryType.Warning)
                  EWS.Diagnostics.Trace.WriteLine(Message, TraceLevel.Info)
               End If
            Next
         End If
      Next

   End Sub

   Private Shared Sub CheckSessionHealth()

      For Each Pool As SessionPool In m_SessionPools.Values
         For Each Session As SessionInstance In Pool.Values
            IsSessionHealthlyResetIfNot(Session)
         Next
      Next

   End Sub


   Private Shared Function IsSessionHealthlyResetIfNot(ByVal Session As SessionInstance) As Boolean
      Dim Reason As String = String.Empty
      If Not Session.IsHealthy(Reason) Then
         EventLogging.Log("Had to shutdown session " & Session.SessionID & " because it was in a non healthy state - " & Reason, "SessionManager", EventLogEntryType.Warning)
         QueueSessionShutdown(Session.SessionID, "Session was not healthy!")
         m_UnhealthySessions += 1
         Return False
      Else
         Return True
      End If
   End Function

   Private Shared Function SessionChecker() As Boolean

      Try

         SessionCheckerNotificationData.UpdateState(ProcessState.Processing)

         Try
            CheckSessionHealth()
            'wait one second for ereything to settle before we try to shutdown sessions
            System.Threading.Thread.Sleep(1000)

         Catch ex As Exception
            EventLogging.Log("EXCEPTION:" & ex.ToString, "SessionManager", EventLogEntryType.Error)
         End Try

         Try
            ShutdownSessionsQueuedForShutdown()
            'wait one second for ereything to settle before we try to startup new sessions
            System.Threading.Thread.Sleep(1000)
         Catch ex As Exception
            EventLogging.Log("EXCEPTION:" & ex.ToString, "SessionManager", EventLogEntryType.Error)
         End Try

         Try
            ReleaseInstructionSetSessionsInUseWhereSessionNotInUse()
            CreateSessionsInPoolsWithoutEnoughAllocated()
         Catch ex As Exception
            EventLogging.Log("EXCEPTION:" & ex.ToString, "SessionManager", EventLogEntryType.Error)
         End Try

         SessionCheckerNotificationData.UpdateState(ProcessState.Running)

      Catch ex As Exception
         EventLogging.Log("EXCEPTION:" & ex.ToString, "SessionManager", EventLogEntryType.Error)
      End Try


      Return True

   End Function

   Private Shared Function SessionCheckerNotificationData() As NotifyData
      Return NotificationManager.Instance(SessionInstance.NOTIFY_CATEGORY, "SessionChecker")
   End Function

   Private Shared m_LastTooFewSessionAvailableMailSentUTC As New Dictionary(Of String, Date)
   Private Shared Sub SendTooFewSessionAvailableAltertEmail(ByVal Available As Integer, ByVal SessionsRequired As Integer, ByVal Pool As String)

      Try
         'only send one an hour
         If m_LastTooFewSessionAvailableMailSentUTC(Pool.ToUpper).AddHours(1) < Date.UtcNow Then

            EventLogging.Log("Have not got enough Sessions ID's to enable the min sessions of " & SessionsRequired & " for pool " & Pool & ".", "SessionManager", EventLogEntryType.Warning)

            EmailSender.SendEMailMessage(MQRConfig.Current.MQRServiceConfig.TooFewSessionsEMailAltertTo, "MQR Too Few Sessions Available Status Alert", "MQR is trying to create a session but there are none available. Pool:" & Pool & " Allocated:" & Available.ToString & " Required: " & SessionsRequired.ToString & ".", True)
            m_LastTooFewSessionAvailableMailSentUTC(Pool.ToUpper) = Date.UtcNow

         End If
      Catch ex As Exception
         EventLogging.Log("Failed sending too few sesssions available altert!" & ControlChars.NewLine & ex.ToString, "LogonCredentialManager", EventLogEntryType.Warning)
      End Try

   End Sub
   Public Shared Sub StopSessionChecker()
      m_SessionCheckerAborted = True
      ThreadManager.ShutdownThread(SessionCheckerThreadName)
      SessionCheckerNotificationData.UpdateState(ProcessState.Stopped)
   End Sub

   Public Shared Sub StartSessionChecker()
      StartSessionCheckerThread()
   End Sub

   Private Shared Function CreateSession(ByVal InstructionSet As LogonInstructionSet) As Boolean

      Dim Result As Boolean = False

      Dim NewSession As SessionInstance = Nothing
      Dim Pool As SessionPool = m_SessionPools(InstructionSet.Identifier.ToUpper)

      Try
         'This will mark the Session in the Instruction Set as being InUse
         Dim InstructionSetSession As LogonSession = InstructionSet.GetFreeSession

         'Check we are not trying to shut this session down, if we are get another
         Do While m_ShutDownQueue.ContainsKey(InstructionSetSession.SessionID) Or _
                  Pool.ContainsKey(InstructionSetSession.SessionID) Or _
                  m_SessionPoolIndexer.ContainsKey(InstructionSetSession.SessionID)

            InstructionSetSession = InstructionSet.GetFreeSession
         Loop

         NewSession = New SessionInstance(InstructionSet, InstructionSetSession)
      Catch e As NoFreeSessionIDsException
         EventLogging.Log("Cannot get a free session ID from InstructionSet " & InstructionSet.Identifier, "SessionManager", EventLogEntryType.Warning)
      End Try

      If Not NewSession Is Nothing Then

         If Pool.ContainsKey(NewSession.SessionID) Then
            QueueSessionShutdown(NewSession.SessionID, "Session already in pool")
         Else
            If m_SessionPoolIndexer.ContainsKey(NewSession.SessionID) Then
               QueueSessionShutdown(NewSession.SessionID, "Session already in pool indexer")
            Else
               Pool.Add(NewSession.SessionID, NewSession)
               m_SessionPoolIndexer.Add(NewSession.SessionID, Pool.PoolID)

               Result = True
               Trace.WriteLine("Created a session " & NewSession.SessionID & " in pool " & Pool.PoolID, TraceLevel.Verbose)
            End If
         End If

      End If

      Return Result

   End Function

   Private Shared m_ServerToMFTimeDiff As TimeSpan = TimeSpan.Zero
   Public Shared ReadOnly Property ServerToMFTimeDiff As TimeSpan
      Get
         Return m_ServerToMFTimeDiff
      End Get
   End Property

   Public Shared Sub UpdateTimeShift(ServerTime As Date, MFTime As Date)

      Dim NewValue As TimeSpan = MFTime - ServerTime

      If Not NewValue.TotalSeconds = m_ServerToMFTimeDiff.TotalSeconds Then
         m_ServerToMFTimeDiff = NewValue
      End If


   End Sub

End Class
