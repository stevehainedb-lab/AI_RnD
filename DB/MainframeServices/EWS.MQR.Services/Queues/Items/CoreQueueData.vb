Public Class CoreQueueData

#Region " Member Variables "

   Private m_RequestID As Guid
   Private m_RequestorConnectionID As Integer
   Private m_LogonIdentifier As String
   Private m_QueryIdentifier As String
   Private m_ParseIdentifier As String
   Private m_RequestSubmitDate As Date
   Private m_RequestSessionIdentifier As String

#End Region

#Region " Constructors "

   Public Sub New(ByVal RequestID As Guid, _
                  ByVal RequestorConnectionID As Integer, _
                  ByVal LogonInstructionSet As String, _
                  ByVal QueryInstructionSet As String, _
                  ByVal ParseInstructionSet As String)

      m_RequestID = RequestId
      m_RequestorConnectionID = RequestorConnectionID
      m_LogonIdentifier = LogonInstructionSet
      m_QueryIdentifier = QueryInstructionSet
      m_ParseIdentifier = ParseInstructionSet

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
         Return m_ParseIdentifier
      End Get
   End Property

   Public ReadOnly Property RequestorConnectionID() As Integer
      Get
         Return m_RequestorConnectionID
      End Get
   End Property

   Public Property RequestSubmitDate() As Date
      Get
         Return m_RequestSubmitDate
      End Get
      Set(ByVal Value As Date)
         m_RequestSubmitDate = Value
      End Set
   End Property

   Public Property RequestSessionIdentifier() As String
      Get
         Return m_RequestSessionIdentifier
      End Get
      Set(ByVal Value As String)
         m_RequestSessionIdentifier = Value
      End Set
   End Property

#End Region

End Class
