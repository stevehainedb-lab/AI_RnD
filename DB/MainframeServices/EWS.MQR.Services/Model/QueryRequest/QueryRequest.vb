Imports EWS.XML
Imports EWS.XML.XMLUtilities

<Serializable(), CLSCompliant(True)> _
Public Class QueryRequest : Inherits XMLBase

   Public Sub New()

   End Sub

   Public Sub New(ByVal Xml As String)
      MyBase.PopulateFromXML(Xml)
   End Sub

   Private m_ID As Guid = Guid.NewGuid
   Public Property ID() As Guid
      Get
         Return m_ID
      End Get
      Set(ByVal value As Guid)
         m_ID = value
      End Set
   End Property

   Private m_Parameters As New QueryRequestParameters
   Public ReadOnly Property Parameters() As QueryRequestParameters
      Get
         Return m_Parameters
      End Get
   End Property

   Private m_LogonInstructionSet As String
   Public Property LogonInstructionSet() As String
      Get
         Return m_LogonInstructionSet
      End Get
      Set(ByVal value As String)
         m_LogonInstructionSet = value
      End Set
   End Property

   Private m_QueryInstructionSet As String
   Public Property QueryInstructionSet() As String
      Get
         Return m_QueryInstructionSet
      End Get
      Set(ByVal value As String)
         m_QueryInstructionSet = value
      End Set
   End Property

   Private m_ParseInstructionSet As String
   Public Property ParseInstructionSet() As String
      Get
         Return m_ParseInstructionSet
      End Get
      Set(ByVal value As String)
         m_ParseInstructionSet = value
      End Set
   End Property

   Private m_ExpectResponse As Boolean = True
   Public Property ExpectResponse() As Boolean
      Get
         Return m_ExpectResponse
      End Get
      Set(ByVal value As Boolean)
         m_ExpectResponse = value
      End Set
   End Property

   Private m_CallingApplication As String = "UNKNOWN"
   Public Property CallingApplication() As String
      Get
         Return m_CallingApplication
      End Get
      Set(ByVal value As String)
         m_CallingApplication = value
      End Set
   End Property

   Private m_TimeOutSeconds As Integer = 0
   Public Property TimeOutSeconds() As Integer
      Get
         Return m_TimeOutSeconds
      End Get
      Set(ByVal value As Integer)
         If value <= 0 Then
            m_TimeOutSeconds = 60
         Else
            m_TimeOutSeconds = value
         End If
      End Set
   End Property
   Public ReadOnly Property TimeOut() As TimeSpan
      Get
         If TimeOutSeconds = 0 Then
            Return TimeSpan.Zero
         Else
            Return New TimeSpan(0, 0, TimeOutSeconds)
         End If
      End Get
   End Property

   Protected Overrides Sub AddMemberDataXmlToXmlWriter(ByVal Writer As System.Xml.XmlWriter)
      Writer.WriteAttributeString("ID", m_ID.ToString)
      Writer.WriteAttributeString("LogonInstructionset", m_LogonInstructionSet)
      Writer.WriteAttributeString("QueryInstructionSet", m_QueryInstructionSet)
      Writer.WriteAttributeString("ParseInstructionSet", m_ParseInstructionSet)
      Writer.WriteAttributeString("ExpectResponse", m_ExpectResponse.ToString)
      Writer.WriteAttributeString("TimeOutSeconds", TimeOutSeconds.ToString)
      Writer.WriteAttributeString("CallingApplication", CallingApplication)
      Parameters.AddXMLToWriter(Writer)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXmlReader(ByVal Reader As System.Xml.XmlReader)
      If Reader.GetAttribute("ID") = String.Empty Then
         m_ID = Guid.Empty
      Else
         m_ID = New Guid(Reader.GetAttribute("ID"))
      End If
      m_LogonInstructionSet = Reader.GetAttribute("LogonInstructionset")
      m_QueryInstructionSet = Reader.GetAttribute("QueryInstructionSet")
      m_ParseInstructionSet = Reader.GetAttribute("ParseInstructionSet")
      If Reader.GetAttribute("ExpectResponse") = String.Empty Then
         m_ExpectResponse = True
      Else
         m_ExpectResponse = CBool(Reader.GetAttribute("ExpectResponse"))
      End If
      If Reader.GetAttribute("TimeOutSeconds") = String.Empty Then
         TimeOutSeconds = 60
      Else
         TimeOutSeconds = CInt(Reader.GetAttribute("TimeOutSeconds"))
      End If
      If Reader.GetAttribute("CallingApplication") = String.Empty Then
         CallingApplication = "UNKNOWN"
      Else
         CallingApplication = Reader.GetAttribute("CallingApplication")
      End If

      Reader.Read()
      Parameters.PopulateFromXMLReader(Reader)
   End Sub

   Public Overrides Function ToString() As String
      Return String.Format("Logon:{0} Query:{1} Parse:{2}", LogonInstructionSet, QueryInstructionSet, ParseInstructionSet)
   End Function

   Public Function UniqueHashCode() As String
      Return String.Format("{0}:{1}:{2}:{3}", LogonInstructionSet, QueryInstructionSet, ParseInstructionSet, Parameters.UniqueString)
   End Function

   'Public Overloads Shared Function GetHashCode(value As String) As Integer
   '   Dim h As Integer = 0
   '   For i As Integer = 0 To value.Length - 1
   '      h += AscW(value(i)) * 31 Xor value.Length - (i + 1)
   '   Next
   '   Return h
   'End Function

End Class

