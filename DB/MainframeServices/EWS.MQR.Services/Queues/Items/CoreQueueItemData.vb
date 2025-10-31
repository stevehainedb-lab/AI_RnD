Public Class CoreQueueItemData

#Region " Member Variables "

   Private m_RequestID As Guid
   Private m_LogonIdentifier As String
   Private m_QueryIdentifier As String
   Private m_QueryParameters As String
   Private m_ParseIdentifier As String
   Private m_RequestSessionIdentifier As String
   Private m_RequestRecievedAtUTC As Date
   Private m_RequestTimeOut As TimeSpan = TimeSpan.Zero
   Private m_MFTimeQuerySubmitted As Date
   Private m_MFTimeResponseRecieved As Date

#End Region

#Region " Constructors "

   Public Shared Function Empty() As CoreQueueItemData
      Dim NewItem As New CoreQueueItemData

      With NewItem
         .m_LogonIdentifier = "UNKNOWN"
         .m_ParseIdentifier = "UNKNOWN"
         .m_QueryIdentifier = "UNKNOWN"
         .m_QueryParameters = "NONE"
         .m_RequestID = New Guid("AAAAAAAA-0000" & Guid.NewGuid.ToString.Substring(13))
         .m_RequestSessionIdentifier = "NONE"
         .m_RequestRecievedAtUTC = Date.MinValue
         .m_RequestTimeOut = TimeSpan.Zero
         .m_MFTimeResponseRecieved = Date.MinValue
         .m_MFTimeQuerySubmitted = Date.MinValue
      End With
      Return NewItem
   End Function


   Private Sub New()

   End Sub

   Public Sub New(ByVal Item As QueryRequest)
      m_RequestID = Item.ID
      m_RequestRecievedAtUTC = Date.UtcNow
      m_RequestTimeOut = Item.TimeOut
      m_LogonIdentifier = Item.LogonInstructionSet
      m_QueryIdentifier = Item.QueryInstructionSet
      m_ParseIdentifier = Item.ParseInstructionSet
      m_QueryParameters = Item.Parameters.ToString
   End Sub

#End Region

#Region " Public Properties "

   Public ReadOnly Property RequestID() As Guid
      Get
         Return m_RequestID
      End Get
   End Property

   Public ReadOnly Property LogonIdentifier() As String
      Get
         Return m_LogonIdentifier
      End Get
   End Property

   Public ReadOnly Property QueryIdentifier() As String
      Get
         Return m_QueryIdentifier
      End Get
   End Property

   Public ReadOnly Property ParseIdentifier() As String
      Get
         If m_ParseIdentifier = String.Empty Then
            m_ParseIdentifier = "RawCapture"
         End If
         Return m_ParseIdentifier
      End Get
   End Property

   Public ReadOnly Property QueryParameters() As String
      Get
         Return m_QueryParameters
      End Get
   End Property

   Public ReadOnly Property RequestRecievedAtUTC() As Date
      Get
         Return m_RequestRecievedAtUtc
      End Get
   End Property

   Public ReadOnly Property RequestTimeOut() As TimeSpan
      Get
         Return m_RequestTimeout
      End Get
   End Property

   Public ReadOnly Property MFTimeResponseRecieved() As Date
      Get
         Return m_MFTimeResponseRecieved
      End Get
   End Property

   Public ReadOnly Property QueryTime() As TimeSpan
      Get
         Return New TimeSpan(m_MFTimeResponseRecieved.Ticks - m_MFTimeQuerySubmitted.Ticks)
      End Get
   End Property

   Public Sub RecievedResponse(MFResponseTime As Date)
      m_MFTimeResponseRecieved = MFResponseTime
   End Sub

   Public Sub QuerySubmitted(RequestSessionIdentifier As String)
      m_RequestSessionIdentifier = RequestSessionIdentifier
      m_MFTimeQuerySubmitted = Now.Add(SessionManager.ServerToMFTimeDiff)
   End Sub

   Public ReadOnly Property MFTimeQuerySubmitted As Date
      Get
         Return m_MFTimeQuerySubmitted
      End Get
   End Property


   Public ReadOnly Property RequestSessionIdentifier() As String
      Get
         Return m_RequestSessionIdentifier
      End Get
   End Property

   Public ReadOnly Property InstructionSetData() As String
      Get
         Return "Logon:" & LogonIdentifier & " Query:" & QueryIdentifier & " Parse:" & ParseIdentifier & " Parameters:" & m_QueryParameters
      End Get
   End Property

#End Region

#Region " Overrides "

   Public Overrides Function ToString() As String
      Return "RequestID:" & RequestID.ToString & " MFSubmitDate " & m_MFTimeQuerySubmitted.ToString("dd/MM/yy HH:mm:ss") & " " & InstructionSetData & " RequetedSessionIdentifer: " & m_RequestSessionIdentifier
   End Function

#End Region

End Class


