Imports EWS.XML

Public Class InstructionSetData : Inherits XMLBase

   Private m_LogonInstructionSetData As New LogonInstructionSetDatas
   Private m_ParseInstructionSetData As New ParseInstructionSetDatas
   Private m_QueryInstructionSetData As New QueryInstructionSetDatas

   Public ReadOnly Property LogonInstructionSetData() As LogonInstructionSetDatas
      Get
         Return m_LogonInstructionSetData
      End Get
   End Property

   Public ReadOnly Property QueryInstructionSetData() As QueryInstructionSetDatas
      Get
         Return m_QueryInstructionSetData
      End Get
   End Property

   Public ReadOnly Property ParseInstructionSetData() As ParseInstructionSetDatas
      Get
         Return m_ParseInstructionSetData
      End Get
   End Property

   Protected Overrides Sub AddMemberDataXMLToXMLWriter(ByVal Writer As System.Xml.XmlWriter)
      m_LogonInstructionSetData.AddXMLToWriter(Writer)
      m_QueryInstructionSetData.AddXMLToWriter(Writer)
      m_ParseInstructionSetData.AddXMLToWriter(Writer)
   End Sub

   Protected Overrides Sub PopulateMemberDataFromXMLReader(ByVal Reader As System.Xml.XmlReader)
      Reader.Read()
      m_LogonInstructionSetData.PopulateFromXMLReader(Reader)
      m_QueryInstructionSetData.PopulateFromXMLReader(Reader)
      m_ParseInstructionSetData.PopulateFromXMLReader(Reader)
   End Sub
End Class
