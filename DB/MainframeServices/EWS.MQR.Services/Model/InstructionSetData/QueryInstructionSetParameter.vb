Imports EWS.XML

Public Class QueryInstructionSetParameter : Inherits XMLBase

   Private m_Name As String

   Public Overrides Function Key() As String
      Return m_Name
   End Function

   Public Property Name() As String
      Get
         Return m_Name
      End Get
      Set(ByVal value As String)
         m_Name = value
      End Set
   End Property

   Protected Overrides Sub AddMemberDataXMLToXMLWriter(ByVal Writer As System.Xml.XmlWriter)
      Writer.WriteAttributeString("Name", m_Name)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXMLReader(ByVal Reader As System.Xml.XmlReader)
      m_Name = Reader.GetAttribute("Name")
      Reader.Read()
   End Sub

End Class
