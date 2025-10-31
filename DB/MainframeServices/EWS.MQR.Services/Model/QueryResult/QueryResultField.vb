Imports EWS.XML
Imports EWS.XML.XMLUtilities

<Serializable()> _
Public Class QueryResultField : Inherits XMLBase

   Private m_Value As String
   Private m_Identifier As String


   Public Property Value() As String
      Get
         Return m_Value
      End Get
      Set(ByVal value As String)
         m_Value = value
      End Set
   End Property

   Public Property Identifier() As String
      Get
         Return m_Identifier
      End Get
      Set(ByVal value As String)
         m_Identifier = value
      End Set
   End Property

   Protected Overrides Sub AddMemberDataXmlToXmlWriter(ByVal Writer As System.Xml.XmlWriter)

      Writer.WriteAttributeString("Identifier", Identifier)
      Writer.WriteAttributeString("Value", Value)

   End Sub

   Protected Overrides Sub PopulateMemberDataFromXmlReader(ByVal Reader As System.Xml.XmlReader)

      Identifier = Reader.GetAttribute("Identifier")
      Value = Reader.GetAttribute("Value")
      Reader.Read()

   End Sub

   Public Overrides Function Key() As String
      Return Identifier
   End Function

End Class
