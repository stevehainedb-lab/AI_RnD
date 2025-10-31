Imports EWS.MQR.XML

Public Class RequestItem : Inherits QueueItemBase

   Private m_Request As QueryRequest

   Public Sub New(ByVal Item As QueryRequest)
      MyBase.New(New CoreQueueItemData(Item), New RequestMetrics)
      m_Request = Item
   End Sub

   Public Function GetFieldValue(ByVal FieldName As String) As String
      If m_Request.Parameters.ContainsKey(FieldName) Then
         Return m_Request.Parameters(FieldName).Value
      Else
         'EventLogging.Log("The parameter " & FieldName & " was not supplied for this query. '' was used as a default!", Me.GetType.Name, EventLogEntryType.Warning)
         Return ""
      End If
   End Function

   Public ReadOnly Property Request() As QueryRequest
      Get
         Return m_Request
      End Get
   End Property

   Public ReadOnly Property QueryInstructionSetToUse() As QueryInstructionSet
      Get
         Return InstructionSetManager.GetQueryInstructionset(QueryIdentifier)
      End Get
   End Property

   Public ReadOnly Property Parameters() As QueryRequestParameters
      Get
         Return m_Request.Parameters
      End Get
   End Property

   Public ReadOnly Property ParametersString() As String
      Get
         Return m_Request.Parameters.ToString
      End Get
   End Property

   Public ReadOnly Property CommandID() As String
      Get
         Return m_Request.Parameters.CommandID
      End Get
   End Property

   Public ReadOnly Property AsAt() As Date
      Get
         Return m_Request.Parameters.AsAt
      End Get
   End Property

   Public ReadOnly Property ExpectResponse() As Boolean
      Get
         Return m_Request.ExpectResponse
      End Get
   End Property

   Public ReadOnly Property CacheTTLSecs As Integer
      Get
         Return m_Request.Parameters.CacheTTLSecs
      End Get
   End Property

   Public Sub QuerySubmitted(ByVal TOPSAltName As String)
      QueueItemData.QuerySubmitted(TOPSAltName)
   End Sub

End Class
