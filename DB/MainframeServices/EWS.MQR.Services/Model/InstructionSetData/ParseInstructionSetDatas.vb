Imports EWS.XML

Public Class ParseInstructionSetDatas : Inherits XMLBaseList(Of ParseInstructionSetData)

   Public Sub New()
      MyBase.New(MultiplicityKind.OneToMany)
      m_HasContainerTags = True
   End Sub

   Public Overrides Function NewItem() As ParseInstructionSetData
      Return New ParseInstructionSetData
   End Function

   Public Function ContainsAll() As Boolean

      Dim Result As Boolean = False

      For Each Item As ParseInstructionSetData In Me
         If Item.Name = "*" Then
            Result = True
            Exit For
         End If
      Next

      Return Result

   End Function

End Class
