Imports EWS.MQR.XML
Imports EWS.Diagnostics
Imports System.Threading

Public Class SessionPool : Inherits Dictionary(Of String, SessionInstance)

   Private m_AvailabilityLock As New Object

   Public Sub New(ByVal PoolID As String)
      m_PoolID = PoolID
   End Sub

   Private m_PoolID As String
   Public Property PoolID() As String
      Get
         Return m_PoolID
      End Get
      Set(ByVal value As String)
         m_PoolID = value
      End Set
   End Property

   Private m_AvailableAfterUtc As Date = Date.MinValue
   Public ReadOnly Property AvailableAfterUtc() As Date
      Get
         Return m_AvailableAfterUtc
      End Get
   End Property

   Public ReadOnly Property AvailableAfterLocal() As Date
      Get
         Return m_AvailableAfterUtc.ToLocalTime
      End Get
   End Property

   Public ReadOnly Property Available() As Boolean
      Get
         Monitor.Enter(m_AvailabilityLock)
         Try
            Return m_AvailableAfterUtc <= Now.ToUniversalTime
         Finally
            Monitor.Exit(m_AvailabilityLock)
         End Try
      End Get
   End Property

   Private m_UnavailablityReason As String = ""
   Public ReadOnly Property UnavailablityReason() As String
      Get
         Return m_UnavailablityReason
      End Get
   End Property

   Public Sub MakeAvailable()
      Monitor.Enter(m_AvailabilityLock)
      Try
         m_AvailableAfterUtc = Date.MinValue
         m_UnavailablityReason = ""
      Finally
         Monitor.Exit(m_AvailabilityLock)
      End Try
   End Sub

   Public Sub MakeUnavailable(ByVal AvailableAfterUtc As Date, ByVal Reason As String)

      EventLogging.Log("Making pool " & m_PoolID & " unavailuble untill " & AvailableAfterUtc.ToLocalTime.ToString("dd/MM/yy HH:mm:ss") & " because " & Reason, Me.GetType.Name, EventLogEntryType.Information)
      Monitor.Enter(m_AvailabilityLock)
      Try
         m_AvailableAfterUtc = AvailableAfterUtc
         m_UnavailablityReason = Reason
         CloseAllSessions(Reason)
      Finally
         Monitor.Exit(m_AvailabilityLock)
      End Try

   End Sub

   Public Sub CloseAllSessions(ByVal Reason As String)

      For Each Item As SessionInstance In Me.Values
         SessionManager.QueueSessionShutdown(Item.SessionID, Reason)
      Next

   End Sub

   Public Function ItemCount() As String
      Return MyBase.Count & " Item(s)"
   End Function

   Public Function GetStateData() As SessionPoolStateDataItem
      Dim State As New SessionPoolStateDataItem

      With State
         .Identifier = PoolID
         .Available = Available
         .UnavailableReason = UnavailablityReason
         .UnavailableUntill = AvailableAfterLocal

         For Each Session As SessionInstance In Me.Values
            .SessionSates.Add(Session.SessionID, Session.GetStateData)
         Next

      End With


      Return State
   End Function

End Class
