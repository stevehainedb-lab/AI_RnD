Imports EWS.XML

Public Class QueryInstructionSetParameters : Inherits XMLBaseList(Of QueryInstructionSetParameter)

   Public Sub New()
      MyBase.New(MultiplicityKind.NoneToMany)
   End Sub

   Public Overrides Function NewItem() As QueryInstructionSetParameter
      Return New QueryInstructionSetParameter
   End Function

End Class
