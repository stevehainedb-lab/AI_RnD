Imports System.Collections
Imports System.Collections.Generic
Imports System.Threading

Public Class SyncLockDictionaryQueue(Of TKey As IComparable(Of TKey), UValue As Class) : Inherits Dictionary(Of TKey, UValue)

   Private _LockAccessor As New Object
   Private _Queue As New LinkedList(Of TKey)

   Private Sub AcquireLock()
      Monitor.Enter(_LockAccessor)
   End Sub

   Private Sub ReleaseLock()
      Monitor.Exit(_LockAccessor)
   End Sub

#Region " Constructors "

   Public Sub New()
      MyBase.New()
   End Sub

   Public Sub New(ByVal copy As Dictionary(Of TKey, UValue))
      MyBase.New(copy)
   End Sub

   Public Sub New(ByVal comparer As IEqualityComparer(Of TKey))
      MyBase.New(comparer)
   End Sub

   Public Sub New(ByVal capacity As Integer)
      MyBase.New(capacity)
   End Sub

   Public Sub New(ByVal capacity As Integer, ByVal comparer As IEqualityComparer(Of TKey))
      MyBase.New(capacity, comparer)
   End Sub

#End Region

#Region " Item Access "

   Public Shadows Sub Add(ByVal key As TKey, ByVal value As UValue)
      AcquireLock()
      Try
         If MyBase.ContainsKey(key) Then
            Throw New ItemAlreadyExistsException("The Item '" & key.ToString & "' Already Exists in this collection!")
         Else
            _Queue.AddLast(key)
            MyBase.Add(key, value)
         End If
      Finally
         ReleaseLock()
      End Try
   End Sub

   Public Shadows Sub AddUpdate(ByVal key As TKey, ByVal value As UValue)
      AcquireLock()
      Try
         If MyBase.ContainsKey(key) Then
            MyBase.Item(key) = value
         Else
            _Queue.AddLast(key)
            MyBase.Add(key, value)
         End If
      Finally
         ReleaseLock()
      End Try
   End Sub

   Public Shadows Sub Remove(ByVal key As TKey)
      Dequeue(key)
   End Sub

   Public Function Dequeue(ByVal key As TKey) As UValue

      Dim Result As UValue = Nothing
      ' get read lock and return if nothing to remove
      AcquireLock()

      Try
         If MyBase.ContainsKey(key) Then
            ' upgrade to writer lock so we can safely remove the item
            ' remove the item
            _Queue.Remove(key)
            Result = MyBase.Item(key)
            MyBase.Remove(key)
         End If
      Finally
         ' release reader lock
         ReleaseLock()
      End Try

      Return Result
   End Function

   Public Function Dequeue() As UValue
      Dim Result As UValue = Nothing
      ' get read lock and return if nothing to remove
      AcquireLock()

      Try
         If _Queue.Count > 0 Then
            ' upgrade to writer lock so we can safely remove the item
            Dim key As TKey = _Queue.First.Value
            _Queue.RemoveFirst()
            Result = MyBase.Item(key)

            ' remove the item
            MyBase.Remove(key)
         End If

      Finally
         ' release reader lock
         ReleaseLock()
      End Try
      Return Result
   End Function

#End Region

   Public Shadows Function ContainsKey(ByVal key As TKey) As Boolean
      AcquireLock()
      Try
         Return MyBase.ContainsKey(key)
      Finally
         ReleaseLock()
      End Try
   End Function

   Public Shadows Function ContainsValue(ByVal value As UValue) As Boolean
      AcquireLock()
      Try
         Return MyBase.ContainsValue(value)
      Finally
         ReleaseLock()
      End Try
   End Function

   Public Shadows Function TryGetValue(ByVal key As TKey, ByRef value As UValue) As Boolean
      AcquireLock()
      Try
         Return MyBase.TryGetValue(key, value)
      Finally
         ReleaseLock()
      End Try
   End Function

   Public Shadows ReadOnly Property Count() As Integer
      Get
         AcquireLock()
         Try
            Return MyBase.Count
         Finally
            ReleaseLock()
         End Try
      End Get
   End Property

   Public Shadows Function GetEnumerator() As SafeEnumerator
      AcquireLock()
      Try

         Dim items As New ArrayList()
         Dim en As Enumerator = MyBase.GetEnumerator()
         While en.MoveNext()
            items.Add(en.Current)
         End While

         Return New SafeEnumerator(items)
      Finally
         ReleaseLock()
      End Try
   End Function

   Public Shadows ReadOnly Property Keys() As SafeEnumerator
      Get
         AcquireLock()
         Try

            Dim items As New ArrayList()
            For Each key As Object In MyBase.Keys
               items.Add(key.ToString())
            Next

            Return New SafeEnumerator(items)
         Finally
            ReleaseLock()
         End Try
      End Get
   End Property

   Public Shadows ReadOnly Property Values() As SafeEnumerator
      Get
         AcquireLock()
         Try

            Dim items As New ArrayList()
            For Each key As Object In MyBase.Values
               items.Add(key)
            Next

            Return New SafeEnumerator(items)
         Finally
            ReleaseLock()
         End Try
      End Get
   End Property

   Default Public Shadows Property Item(ByVal key As TKey) As UValue
      Get
         AcquireLock()
         Try
            Dim value As UValue = Nothing
            If MyBase.TryGetValue(key, value) Then
               Return value
            Else
               Return Nothing
            End If
         Finally
            ReleaseLock()
         End Try
      End Get
      Set(ByVal value As UValue)
         AcquireLock()
         Try
            If MyBase.ContainsKey(key) Then
               MyBase.Item(key) = value
            Else
               MyBase.Add(key, value)
            End If
         Finally
            ReleaseLock()
         End Try
      End Set
   End Property

   Public ReadOnly Property SyncRoot() As Object
      Get
         Return Me
      End Get
   End Property

End Class

Public Class SafeEnumerator
   Implements System.Collections.IEnumerator
   Implements IEnumerable
   Private _items As IList
   Private _current As Integer

   Public Sub New(ByVal items As IList)
      _items = items
      _current = -1
   End Sub

   Public ReadOnly Property Current() As Object
      Get
         If _current < 0 OrElse 0 = _items.Count Then
            Return Nothing
         End If

         Return _items(_current)
      End Get
   End Property

   Public Sub Dispose()
   End Sub

   Private ReadOnly Property IEnumerator_Current() As Object Implements IEnumerator.Current
      Get
         Return Me.Current
      End Get
   End Property

   Public Function MoveNext() As Boolean Implements IEnumerator.MoveNext
      If _current >= _items.Count Then
         Return False
      End If

      _current += 1
      If _current < _items.Count Then
         Return True
      End If

      Return False
   End Function

   Public Sub Reset() Implements IEnumerator.Reset
      _current = -1
   End Sub

   Private Function IEnumerable_GetEnumerator() As IEnumerator Implements IEnumerable.GetEnumerator
      Return _items.GetEnumerator()
   End Function
End Class