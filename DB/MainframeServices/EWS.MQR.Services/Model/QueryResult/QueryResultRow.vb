Imports EWS.XML
Imports EWS.XML.XMLUtilities

<Serializable()> _
Public Class QueryResultRow : Inherits XMLBase

   Private m_Identifier As String
   Private m_FullLine As String
   Private m_Fields As New QueryResultFields

   Public Sub New()

   End Sub

   Public Property Identifier() As String
      Get
         Return m_Identifier
      End Get
      Set(ByVal value As String)
         m_Identifier = value
      End Set
   End Property

   Public Property FullLine() As String
      Get
         Return m_FullLine
      End Get
      Set(ByVal value As String)
         m_FullLine = value
      End Set
   End Property

   Public ReadOnly Property Fields() As QueryResultFields
      Get
         Return m_Fields
      End Get
   End Property

   Protected Overrides Sub AddMemberDataXmlToXmlWriter(ByVal Writer As System.Xml.XmlWriter)
      WriteStringAttribute(Writer, "Identifier", m_Identifier)
      WriteStringAttribute(Writer, "FullLine", FullLine)
      m_Fields.AddXMLToWriter(Writer)

   End Sub

   Protected Overrides Sub PopulateMemberDataFromXmlReader(ByVal Reader As System.Xml.XmlReader)


      m_Identifier = Reader.GetAttribute("Identifier")
      FullLine = Reader.GetAttribute("FullLine")

      Reader.Read()

      m_Fields.PopulateFromXMLReader(Reader)
   End Sub

   Public Overrides Function Key() As String
      Return Identifier
   End Function

   Public Overrides Function ToString() As String
      Return Key()
   End Function

End Class
