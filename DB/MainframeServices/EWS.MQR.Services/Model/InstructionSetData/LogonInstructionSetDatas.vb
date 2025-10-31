Imports EWS.XML

Public Class LogonInstructionSetDatas : Inherits XMLBaseList(Of LogonInstructionSetData)

   Public Sub New()
      MyBase.New(MultiplicityKind.OneToMany)
      m_HasContainerTags = True
   End Sub

   Public Overrides Function NewItem() As LogonInstructionSetData
      Return New LogonInstructionSetData
   End Function

End Class
