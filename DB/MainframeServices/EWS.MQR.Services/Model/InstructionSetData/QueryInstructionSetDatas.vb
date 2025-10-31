Imports EWS.XML

Public Class QueryInstructionSetDatas : Inherits XMLBaseList(Of QueryInstructionSetData)

   Public Sub New()
      MyBase.New(MultiplicityKind.OneToMany)
      m_HasContainerTags = True
   End Sub

   Public Overrides Function NewItem() As QueryInstructionSetData
      Return New QueryInstructionSetData
   End Function

   Public Function ContainsAll() As Boolean

      Dim Result As Boolean = False

      For Each Item As QueryInstructionSetData In Me
         If Item.Name = "*" Then
            Result = True
            Exit For
         End If
      Next

      Return Result

   End Function

End Class
